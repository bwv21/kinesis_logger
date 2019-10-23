using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using KLogger.Configs;
using KLogger.Cores.Structures;
using KLogger.Libs;
using KLogger.Types;

namespace KLogger.Cores.Loggers
{
    /// <summary>
    ///     Logger의 Tick(루프 한 번의 동작).
    ///     로그를 모으고 가공한다.
    /// </summary>
    internal partial class Logger
    {
        #region ThreadLocals

        private class WatcherCounter
        {
            public readonly Dictionary<String, Int32> LogTypeToCount = new Dictionary<String, Int32>();
            public Int32 SendBytes;
            public Int32 LogCount;
            public Int32 DropLogCount;

            public void Clear()
            {
                LogTypeToCount.Clear();
                SendBytes = 0;
                LogCount = 0;
                DropLogCount = 0;
            }
        }

        // 루프마다 new하지 않기 위해 멤버로 가진다. 한 틱 동안만 유효한 임시 변수가 된다.
        private readonly ThreadLocal<List<Log>> _threadLocalLogs = new ThreadLocal<List<Log>>(() => new List<Log>(Const.MAX_KINESIS_BATCH_SIZE));
        private readonly ThreadLocal<Stopwatch> _threadLocalStopwatch = new ThreadLocal<Stopwatch>(() => new Stopwatch());
        private readonly ThreadLocal<WatcherCounter> _threadLocalWatcherCounter = new ThreadLocal<WatcherCounter>(() => new WatcherCounter());

        private void ClearAllThreadLocal()
        {
            _threadLocalLogs.Value.Clear();
            _threadLocalWatcherCounter.Value.Clear();
        }

        #endregion

        private void Tick()
        {
            if (CheckTickCondition() == false)
            {
                return;
            }

            Boolean workThisTick = false;

            // ConcurrentQueue지만 락을 사용한다.
            // 락을 사용하지 않고 여러 스레드가 큐에서 로그를 가져가면, 배치 안의 로그 수가 줄어들어 효율이 떨어진다.
            // 다른 스레드에서의 Push는 큐 뒤에 쌓이므로 상관없다.
            // 다른 스레드에서의 Pop은 강제로 큐를 비우는 경우에 발생할 수 있다(팝하고 개수가 0보다 큰지 확인).
            lock (_tickLock)
            {
                _threadLocalStopwatch.Value.Restart();

                Int64 nowMS = Now.TimestampMS();
                if (IsLongTimePut(nowMS))
                {
                    UpdateLastWorkTime(nowMS);

                    // 로그가 충분하지 않을수도 있지만 보낸지 오래 되었으므로 모인 것을 보낸다.
                    Int32 popCount = CalculatePopCount();
                    workThisTick = 0 < ClearAndPopLogToThreadLocal(popCount);
                }
                else
                {
                    workThisTick = TryPopLogToThreadLocal();
                    if (workThisTick)
                    {
                        UpdateLastWorkTime(nowMS);
                    }
                }

                _threadLocalStopwatch.Value.Stop();
            }

            if (workThisTick)
            {
                ProcessThreadLocal();
            }
        }

        private Boolean CheckTickCondition()
        {
            if (State != StateType.Start)
            {
                DebugLog.Log($"Skip Tick. State is not Start({State.ToString()}). Pending LogCount: {Watcher.PendingLogCount.ToString()}", "klogger:tick");
                return false;
            }

            return true;
        }

        private Boolean IsLongTimePut(Int64 nowMS)
        {
            return Config.MaxBatchWaitTimeMS <= nowMS - _lastWorkTimeMS;
        }

        private void UpdateLastWorkTime(Int64 nowMS)
        {
            _lastWorkTimeMS = nowMS;
        }

        private Boolean TryPopLogToThreadLocal()
        {
            Int32 popCount = CalculatePopCount();

            if (IsEnoughLog(popCount) == false)
            {
                return false;
            }

            if (ClearAndPopLogToThreadLocal(popCount) <= 0)
            {
                return false;   // 다른 스레드에서 빼내서 보낼 것이 없다.
            }

            return true;
        }

        private Int32 CalculatePopCount()
        {
            return Math.Min(_logQueue.Count, Config.MaxBatchSize);
        }

        private Boolean IsEnoughLog(Int32 logCount)
        {
            return Config.MaxBatchSize <= logCount;
        }

        private Int32 ClearAndPopLogToThreadLocal(Int32 popCount)
        {
            ClearAllThreadLocal();

            for (Int32 i = 0; i < popCount; ++i)
            {
                Log log = _logQueue.Pop();
                if (log != null)
                {
                    _threadLocalLogs.Value.Add(log);
                }
            }

            return _threadLocalLogs.Value.Count;
        }

        // 스레드로컬에 복사한 상태이므로 락 바깥에서 호출할 수 있다.
        private void ProcessThreadLocal()
        {
            DebugLog.Log($"ProcessThreadLocal: {_threadLocalLogs.Value.Count.ToString()}", "klogger:tick");

            PutLog putLog = ConvertThreadLocalLogsToPutLogs();

            PutKinesis(putLog);

            UpdateWatcherCounter();

            ClearAllThreadLocal();
        }

        private PutLog ConvertThreadLocalLogsToPutLogs()
        {
            if (_threadLocalLogs.Value.Count <= 0)
            {
                return null;
            }

            Int32 count = _threadLocalLogs.Value.Count;
            var rawLogs = new List<ILog>(count);
            var encodedLogs = new List<Byte[]>(count);
            Int32 totalEncodedLogByte = 0;

            for (Int32 i = 0; i < count; ++i)
            {
                Log log = _threadLocalLogs.Value[i];
                Byte[] encodedLog = LogEncoder.Encode(log);

                CompletePutNoticeResultType completePutNoticeResultType = ValidateEncodedLog(encodedLog);
                if (completePutNoticeResultType != CompletePutNoticeResultType.Success)
                {
                    DropLog(log, completePutNoticeResultType);
                    continue;
                }

                rawLogs.Add(log);
                encodedLogs.Add(encodedLog);
                totalEncodedLogByte += encodedLog.Length;

                UpdateThreadLocalWatcherCounter(log.LogType, encodedLog.Length);
            }

            return new PutLog(rawLogs.ToArray(), encodedLogs.ToArray(), totalEncodedLogByte);
        }

        private CompletePutNoticeResultType ValidateEncodedLog(Byte[] encodedLog)
        {
            if (encodedLog == null)
            {
                return CompletePutNoticeResultType.FailEncode;
            }

            if (CheckTooLargeLogSize(encodedLog))
            {
                return CompletePutNoticeResultType.TooLargeLogSize;
            }

            return CompletePutNoticeResultType.Success;
        }

        private Boolean CheckTooLargeLogSize(Byte[] encodedLog)
        {
            // 직접 1바이트씩 추가하며 테스트.
            // encodedLog.Length 이 1048577 이 되는 순간 실패(1048576 까진 괜찮음).
            // Post 변환 전 데이터로 계산한다.
            return Config.MaxRecordByte < encodedLog.Length;
        }

        private void DropLog(Log log, CompletePutNoticeResultType completePutNoticeResultType)
        {
            CompletePutNotifier.Push(new ILog[] { log }, completePutNoticeResultType);

            _threadLocalWatcherCounter.Value.DropLogCount += 1;

            Reporter.Error($"Fail Encode Log. LogType: {log.LogType}, Reason: {completePutNoticeResultType}");
        }

        private void UpdateThreadLocalWatcherCounter(String logType, Int32 sendBytes)
        {
            if (_threadLocalWatcherCounter.Value.LogTypeToCount.ContainsKey(logType))
            {
                ++_threadLocalWatcherCounter.Value.LogTypeToCount[logType];
            }
            else
            {
                _threadLocalWatcherCounter.Value.LogTypeToCount.Add(logType, 1);
            }

            _threadLocalWatcherCounter.Value.LogCount += 1;
            _threadLocalWatcherCounter.Value.SendBytes += sendBytes;
        }

        private void PutKinesis(PutLog putLog)
        {
            if (putLog == null || putLog.RawLogs.Length <= 0 || putLog.EncodedLogs.Length <= 0)
            {
                return;
            }

            if (State == StateType.Stopping)
            {
                Putter.Put(putLog);
                return;
            }

            if (Config.UseThroughputControl == 1)
            {
                ThroughputController.Push(putLog);
            }
            else
            {
                Putter.Put(putLog);
            }
        }

        private void UpdateWatcherCounter()
        {
            Watcher.UpdateCounter(_threadLocalWatcherCounter.Value.LogTypeToCount,
                                  _threadLocalWatcherCounter.Value.LogCount,
                                  _threadLocalWatcherCounter.Value.DropLogCount,
                                  _threadLocalWatcherCounter.Value.SendBytes,
                                  _threadLocalStopwatch.Value.ElapsedMilliseconds);
        }

        private void TickForced()
        {
            Int32 popLogCount = 0;

            lock (_tickLock)
            {
                popLogCount = ClearAndPopLogToThreadLocal(CalculatePopCount());
            }

            if (0 < popLogCount)
            {
                ProcessThreadLocal();
            }
        }
    }
}

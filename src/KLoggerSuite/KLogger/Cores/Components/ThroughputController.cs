using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using KLogger.Configs;
using KLogger.Cores.Exceptions;
using KLogger.Cores.Loggers;
using KLogger.Libs;
using KLogger.Libs.AWS.Kinesis.Describe;
using KLogger.Types;
using Newtonsoft.Json;

namespace KLogger.Cores.Components
{
    /// <summary>
    /// <para> 1초에 샤드 하나가 받을 수 있는 바이트와 레코드의 제한을 여기서 용량(Capacity)이라 부른다. </para>
    /// <para> 경과 시간에 비례해 용량을 획득하며, 내부 또는 외부에서 전송이 일어나면 용량을 소모하게 된다(용량을 초과하면 재시도하기 때문에 정확히 맞출 필요는 없다). </para>
    /// <para> 최대한 많이 보내서 스루풋 초과에 의해 여러 번 재시도가 일어나는 것보다 제어를 사용하는 것이 같은 시간에 더 많은 양을 보낼 수 있었다. </para>
    /// </summary>
    internal class ThroughputController
    {
        private const Double ONE_SECOND_MS = 1000;                      // 1초, 1000ms.
        private const Int32 THROUGHPUT_CONTROL_MS = 500;                // 컨트롤 주기(짧으면 너무 쪼개서 보내려 한다. 500정도가 적당해 보임).
        private const Int32 UPDATE_SHARD_COUNT_INTERVAL_SEC = 60;       // 샤드 개수 업데이트 주기(초).

        // 너무 작은 단위로 전송하는 것을 막기 위해 사용할 수 있는 최소 사용 용량을 정한다.
        private const Int32 MIN_USABLE_BYTE_CAPACITY = 1024;
        private const Int32 MIN_USABLE_RECORD_CAPACITY = 10;

        private readonly Object _lock = new Object();

        private readonly Action<PutLog> _putAction;
        private readonly ErrorCounter _errorCounter;
        private readonly DescribeStreamAPI _describeStreamAPI;

        private QueueMT<PutLog> _putLogs;
        private List<PutLog> _remainPutLogs;
        private NaiveLoopThread _thread;

        private Int32 _shardCount;
        private DateTime _lastReadShardCountTime;

        private DateTime _lastGainCapacity;

        private Boolean _isThrottling;

        private Int64 _byteCapacity;
        private Int32 _recordCapacity;

        internal Int32 LogCountInQueue => _putLogs?.Count ?? 0;

        internal ThroughputController(Action<PutLog> putAction, Logger logger)
        {
            _putAction = putAction;
            _errorCounter = logger.ErrorCounter;
            _describeStreamAPI = new DescribeStreamAPI(logger.Config.AWSs.Region,
                                                       logger.Config.AWSs.DecryptedAccessID,
                                                       logger.Config.AWSs.DecryptedSecretKey,
                                                       logger.Config.AWSs.KinesisStreamName,
                                                       OnDescribeStreamRequestCompleted);
            _shardCount = 1;
        }

        internal void Push(PutLog putLog)
        {
            _putLogs.Push(putLog);
            DebugLog.Log($"{nameof(ThroughputControl)} Push: {putLog.TotalEncodedLogByte}, {putLog.EncodedLogs.Length}", "klogger:throughput-control");
        }

        internal void UseCapacity(Int64 useByteCapacity, Int32 useRecordCapacity)
        {
            Int64 newByteCapacity = _byteCapacity - useByteCapacity;
            Int32 newRecordCapacity = _recordCapacity - useRecordCapacity;

            newByteCapacity = Math.Max(newByteCapacity, 0);
            newRecordCapacity = Math.Max(newRecordCapacity, 0);

            Int32 shardCount = Math.Max(1, _shardCount);
            Int64 maxByteCapacity = Const.BYTE_FOR_SECOND_PER_SHARD_BYTE * shardCount;
            Int32 maxRecordCapacity = Const.RECORD_FOR_SECOND_PER_SHARD_COUNT * shardCount;

            newByteCapacity = Math.Min(newByteCapacity, maxByteCapacity);
            newRecordCapacity = Math.Min(newRecordCapacity, maxRecordCapacity);

            DebugLog.Log($"OutsideUseCapacity({useByteCapacity}, {useRecordCapacity}: {_byteCapacity} -> {newByteCapacity}, {_recordCapacity} -> {newRecordCapacity}", "klogger:throughput-control");

            Interlocked.Exchange(ref _byteCapacity, newByteCapacity);
            Interlocked.Exchange(ref _recordCapacity, newRecordCapacity);
        }

        internal void EnableThrottling(Boolean isThrottling)
        {
            _isThrottling = isThrottling;
        }

        internal void Start()
        {
            lock (_lock)
            {
                if (_thread != null)
                {
                    throw new LoggerException($"Fail {nameof(ThroughputController)}::{nameof(Start)} {nameof(_thread)} is not null");
                }

                _putLogs = new QueueMT<PutLog>();
                _remainPutLogs = new List<PutLog>();

                _isThrottling = false;

                // 샤드가 최소 1개는 존재할 것이므로 1개 기준으로 초기화.
                _byteCapacity = Const.BYTE_FOR_SECOND_PER_SHARD_BYTE;
                _recordCapacity = Const.RECORD_FOR_SECOND_PER_SHARD_COUNT;

                _thread = new NaiveLoopThread(() => ThroughputControl(DateTime.UtcNow), THROUGHPUT_CONTROL_MS, e => _errorCounter.RaiseError(e), nameof(ThroughputController));
                _thread.Start();
            }
        }

        internal void Stop()
        {
            lock (_lock)
            {
                if (_thread == null)
                {
                    throw new LoggerException($"Fail {nameof(ThroughputController)}::{nameof(Stop)}");
                }

                _thread.Stop();
                _thread = null;

                Flush();
            }
        }

        private void Flush()
        {
            while (true)
            {
                PutLog putLog = _putLogs.Pop();
                if (putLog == null)
                {
                    break;
                }

                Put(putLog);
            }

            foreach (PutLog reservedPutLog in _remainPutLogs)
            {
                Put(reservedPutLog);
            }

            _remainPutLogs.Clear();
        }

        private void Put(PutLog putLog)
        {
            _putAction(putLog);
        }

        private void ThroughputControl(DateTime now)
        {
            GainCapacity(now);
            PutProcess();
            RequestReadShardCount(now);
        }

        private void GainCapacity(DateTime now)
        {
            Int32 elapsedMS = CalculateElapsedMS(now);
            
            Int64 newByteCapacity = CalculateByteCapacity(elapsedMS, _shardCount);
            Int32 newRecordCapacity = CalculateRecordCapacity(elapsedMS, _shardCount);

            DebugLog.Log($"GainCapacity({elapsedMS}ms): {_byteCapacity} -> {newByteCapacity}, {_recordCapacity} -> {newRecordCapacity}", "klogger:throughput-control");

            Interlocked.Exchange(ref _byteCapacity, newByteCapacity);
            Interlocked.Exchange(ref _recordCapacity, newRecordCapacity);

            _lastGainCapacity = now;
        }

        private Int32 CalculateElapsedMS(DateTime now)
        {
            TimeSpan diff = now - _lastGainCapacity;

            // 샤드의 용량 단위 시간이 1초이므로 최댓값은 1초(1초가 넘는다고 그 이상의 용량이 생기지 않으므로).
            Int32 elapsedMS = Math.Min((Int32)ONE_SECOND_MS, (Int32)diff.TotalMilliseconds);
            elapsedMS = Math.Max(0, elapsedMS);

            return elapsedMS;
        }

        // 경과 시간에 비례하여 바이트 용량을 얻는다.
        private Int64 CalculateByteCapacity(Int32 elapsedMS, Int32 shardCount)
        {
            Int64 addByteCapacityPerShard = (Int64)((elapsedMS / ONE_SECOND_MS) * Const.BYTE_FOR_SECOND_PER_SHARD_BYTE);
            Int64 addByteCapacity = addByteCapacityPerShard * shardCount;

            Int64 maxByteCapacity = Const.BYTE_FOR_SECOND_PER_SHARD_BYTE * shardCount;

            return Math.Min(maxByteCapacity, _byteCapacity + addByteCapacity);
        }

        // 경과 시간에 비례하여 레코드 용량을 얻는다.
        private Int32 CalculateRecordCapacity(Int32 elapsedMS, Int32 shardCount)
        {
            Int32 addRecordCapacityPerShard = (Int32)((elapsedMS / ONE_SECOND_MS) * Const.RECORD_FOR_SECOND_PER_SHARD_COUNT);
            Int32 addRecordCapacity = addRecordCapacityPerShard * shardCount;

            Int32 maxRecordCapacity = Const.RECORD_FOR_SECOND_PER_SHARD_COUNT * shardCount;

            return Math.Min(maxRecordCapacity, _recordCapacity + addRecordCapacity);
        }

        private void PutProcess()
        {
            PutFragmentRemainPutLog();
            PutNoFragment();
        }

        // 이전 틱에 보내지 못했던 로그를 분할해서라도 보낸다.
        private void PutFragmentRemainPutLog()
        {
            if (_remainPutLogs.Count <= 0)
            {
                return;
            }

            var nextRemainPutLogs = new List<PutLog>();
            foreach (PutLog putLog in _remainPutLogs)
            {
                Tuple<PutLog, PutLog> usableAndNotUsable = SplitUsableAndNotUsablePutLog(putLog);

                PutLog usablePutLog = usableAndNotUsable.Item1;
                PutLog notUsablePutLog = usableAndNotUsable.Item2;

                if (0 < usablePutLog.TotalEncodedLogByte)
                {
                    DebugLog.Log($"{nameof(ThroughputControl)} FragmentPut: {usablePutLog.TotalEncodedLogByte}, {usablePutLog.EncodedLogs.Length}", "klogger:throughput-control");
                    Put(usablePutLog);
                }

                if (0 < notUsablePutLog.TotalEncodedLogByte)
                {
                    // 이번에도 보내지 못한 로그는 다음에 다시 시도한다.
                    nextRemainPutLogs.Add(notUsablePutLog);
                }
            }

            _remainPutLogs = nextRemainPutLogs;
        }

        // 보낼 수 있는 로그와 보낼 수 없는 로그로 나눈다.
        private Tuple<PutLog, PutLog> SplitUsableAndNotUsablePutLog(PutLog putLog)
        {
            var usableRawLogs = new List<ILog>();
            var usableEncodedLogs = new List<Byte[]>();
            Int32 usableTotalEncodedLogByte = 0;

            var notUsableRawLogs = new List<ILog>();
            var notUsableEncodedLogs = new List<Byte[]>();
            Int32 notUsableTotalEncodedLogByte = 0;

            Int64 useByteCapacity = 0;
            Int32 useRecordCapacity = 0;
            for (Int32 i = 0; i < putLog.EncodedLogs.Length; ++i)
            {
                ILog rawLog = putLog.RawLogs[i];
                Byte[] encodedLog = putLog.EncodedLogs[i];

                if (IsValidLogSize(encodedLog.Length) == false)
                {
                    continue; // 로그를 버린다.
                }

                if (CheckThroughputCondition() &&
                    CheckRemainCapacity(useByteCapacity + encodedLog.Length, useRecordCapacity + 1) && 
                    CheckMaxKinesisBatchSize(usableRawLogs.Count))
                {
                    useByteCapacity += encodedLog.Length;
                    ++useRecordCapacity;

                    usableRawLogs.Add(rawLog);
                    usableEncodedLogs.Add(encodedLog);
                    usableTotalEncodedLogByte += encodedLog.Length;
                }
                else
                {
                    notUsableRawLogs.Add(rawLog);
                    notUsableEncodedLogs.Add(encodedLog);
                    notUsableTotalEncodedLogByte += encodedLog.Length;
                }
            }

            var usablePutLog = new PutLog(usableRawLogs.ToArray(), usableEncodedLogs.ToArray(), usableTotalEncodedLogByte);
            var notUsablePutLog = new PutLog(notUsableRawLogs.ToArray(), notUsableEncodedLogs.ToArray(), notUsableTotalEncodedLogByte);
            return new Tuple<PutLog, PutLog>(usablePutLog, notUsablePutLog);
        }

        private Boolean CheckThroughputCondition()
        {
            if (_isThrottling)
            {
                return false;
            }

            if (_byteCapacity < MIN_USABLE_BYTE_CAPACITY)
            {
                return false;
            }

            if (_recordCapacity < MIN_USABLE_RECORD_CAPACITY)
            {
                return false;
            }

            return true;
        }

        private Boolean IsValidLogSize(Int32 logSize)
        {
            // 버그가 아니라면 로그 처리 앞단에서 걸러지므로 이런 일은 있을 수 없다(버그 알림을 위한 로직).
            // 샤드가 1개면 영원히 큐에 남게된다(1개의 용량보다 크므로 보내지 못함).
            // 샤드가 2개 이상이면 보내는 시도는 하지만 Kinesis의 제약(1MB)에 의해 계속 에러가 발생하고 재시도하다가 로그를 버리게 된다.
            // 이 함수에 의해 로그가 버려지면 위의 일은 일어나지 않는다.
            if (Const.MAX_KINESIS_RECORD_BYTE < logSize)
            {
                _errorCounter.RaiseError($"{nameof(ThroughputControl)}::{nameof(IsValidLogSize)}: Invalid LogSize: {Const.MAX_KINESIS_RECORD_BYTE} < {logSize}");
                return false;
            }

            return true;
        }

        private Boolean CheckRemainCapacity(Int64 useByteCapacity, Int32 useRecordCapacity)
        {
            return useByteCapacity <= _byteCapacity && useRecordCapacity <= _recordCapacity;
        }

        private Boolean CheckMaxKinesisBatchSize(Int32 batchSize)
        {
            return batchSize < Const.MAX_KINESIS_BATCH_SIZE;
        }

        // 로그를 분할하지 않고 최대한 보낸다.
        private void PutNoFragment()
        {
            Int32 loopCount = _putLogs.Count;
            for (Int32 i = 0; i < loopCount; ++i)
            {
                PutLog putLog = _putLogs.Pop();
                if (putLog == null)
                {
                    DebugLog.Log($"{nameof(PutNoFragment)} PutLog is null(Error).", "klogger:throughput-control");
                    return;  // 외부 스레드에서 빼내지 않는한 이럴 수 없음.
                }

                if (CheckThroughputCondition() && 
                    CheckRemainCapacity(putLog.TotalEncodedLogByte, putLog.EncodedLogs.Length))
                {
                    DebugLog.Log($"{nameof(ThroughputControl)} No FragmentPut: {putLog.TotalEncodedLogByte}, {putLog.EncodedLogs.Length}", "klogger:throughput-control");
                    Put(putLog);
                }
                else
                {
                    // 다음 틱에 분할해서라도 보내게 된다.
                    AddRemainPutLog(putLog);
                }
            }
        }

        private void AddRemainPutLog(PutLog putLog)
        {
            _remainPutLogs.Add(putLog);
        }

        // 실시간으로 업데이트 할 필요는 없으므로 요청만하고 콜백을 기다린다.
        private void RequestReadShardCount(DateTime now)
        {
            TimeSpan diff = now - _lastReadShardCountTime;
            if (UPDATE_SHARD_COUNT_INTERVAL_SEC < diff.TotalSeconds)
            {
                _describeStreamAPI.DescribeStream();
                _lastReadShardCountTime = now;
            }
        }

        // 샤드 업데이트 콜백.
        private void OnDescribeStreamRequestCompleted(UploadDataCompletedEventArgs args, Object _)
        {
            try
            {
                if (args == null)
                {
                    return;
                }

                if (args.Error != null)
                {
                    return;
                }

                if (args.Result == null)
                {
                    return;
                }

                var response = JsonConvert.DeserializeObject<ResponseDescribeStreamSummary>(Encoding.UTF8.GetString(args.Result));
                if (response == null || response.StreamDescriptionSummary == null)
                {
                    return;
                }

                if (response.StreamDescriptionSummary.OpenShardCount <= 0)
                {
                    return;
                }

                DebugLog.Log($"{nameof(ThroughputControl)} UpdateShardCount: {_shardCount} -> {response.StreamDescriptionSummary.OpenShardCount}", "klogger:throughput-control");

                Interlocked.Exchange(ref _shardCount, response.StreamDescriptionSummary.OpenShardCount);
            }
            catch (Exception exception)
            {
                _errorCounter.RaiseError(exception.Message);
            }
        }
    }
}

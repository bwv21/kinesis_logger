using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using KLogger.Configs;
using KLogger.Cores.Exceptions;
using KLogger.Cores.Loggers;
using KLogger.Libs;
using KLogger.Libs.AWS.Kinesis.Put;
using KLogger.Types;
using Newtonsoft.Json;

namespace KLogger.Cores.Components
{
    // Monitor, Counter을 피해서 사용한 이름.
    internal class Watcher
    {
        private const Int32 QUEUE_SIZE_RECORD_INTERVAL_MS = 333;            // 큐 크기 기록 주기.

        // 보고가 끝나면 초기화.
        private class TemporalCounter
        {
            internal readonly List<Int32> QueueCounts = new List<Int32>();    // 큐에 있는 로그 수들.
            internal Int32 LogCount;                                          // 전송한 로그 개수.
            internal Int32 DropLogCount;                                      // 버린 로그 개수.
            internal Int32 UpdateCount;                                       // 업데이트 횟수.
            internal Int32 SendBytes;                                         // 전송한 양.
            internal Double ElapsedMSTotal;                                   // 전송에 소모한 시간.
            internal Double ElapsedMSPeak;                                    // 최대 루프 소모 시간.
            internal Int32 QueueCountPeak;                                    // 최대 큐 크기.
            internal Boolean ForceReportFlag;                                 // 조건과 상관없이 한번 슬랙으로 전송하는 플래그.

            public void Clear()
            {
                QueueCounts.Clear();
                LogCount = 0;
                DropLogCount = 0;
                UpdateCount = 0;
                SendBytes = 0;
                ElapsedMSTotal = 0;
                ElapsedMSPeak = 0;
                QueueCountPeak = 0;
                ForceReportFlag = false;
            }
        }

        private readonly Object _lock = new Object();
        private readonly TemporalCounter _temporalCounter = new TemporalCounter();

        private Logger _logger;
        private NaiveLoopThread _mainThread;
        private NaiveLoopThread _recordLogQueueThread;
        private DateTime _lastWatchingTime;
        private Int64 _pushLogCount;
        private Int64 _requestPutLogCount;
        private Int64 _successLogCount;
        private Int64 _pendingLogCount;
        private Int64 _failLogCount;

        internal Int64 PushLogCount => _pushLogCount;
        internal Int64 RequestPutLogCount => _requestPutLogCount;
        internal Int64 SuccessLogCount => _successLogCount;
        internal Int64 PendingLogCount => _pendingLogCount;
        internal Int64 FailLogCount => _failLogCount;

        internal ConcurrentDictionary<String, Int32> LogTypeToCount { get; private set; }
        internal ConcurrentDictionary<String, Int32> KinesisShardToCount { get; private set; }

        internal void Initialize(Logger logger)
        {
            lock (_lock)
            {
                _temporalCounter.Clear();
                _logger = logger;
                _mainThread = null;
                _recordLogQueueThread = null;
                _lastWatchingTime = DateTime.UtcNow;
                _pushLogCount = 0;
                _requestPutLogCount = 0;
                _pendingLogCount = 0;
                _successLogCount = 0;
                _failLogCount = 0;
                LogTypeToCount = new ConcurrentDictionary<String, Int32>();
                KinesisShardToCount = new ConcurrentDictionary<String, Int32>();
            }
        }

        internal void Start()
        {
            lock (_lock)
            {
                if (_mainThread != null || _recordLogQueueThread != null)
                {
                    throw new LoggerException($"Fail {nameof(Watcher)}::{nameof(Start)}");
                }

                _mainThread = new NaiveLoopThread(() => Watching(DateTime.UtcNow), _logger.Config.Watchers.IntervalMS, e => _logger.ErrorCounter.RaiseError(e), $"{nameof(Watcher)}-Watching");
                _recordLogQueueThread = new NaiveLoopThread(RecordLogQueue, QUEUE_SIZE_RECORD_INTERVAL_MS, e => _logger.ErrorCounter.RaiseError(e), $"{nameof(Watcher)}-Record");

                _mainThread.Start();
                _recordLogQueueThread.Start();
            }
        }

        internal void Stop()
        {
            lock (_lock)
            {
                if (_mainThread == null || _recordLogQueueThread == null)
                {
                    throw new LoggerException($"Fail {nameof(Watcher)}::{nameof(Stop)}");
                }

                _mainThread.Stop();
                _recordLogQueueThread.Stop();

                _mainThread = null;
                _recordLogQueueThread = null;
            }
        }

        internal void IncrementPushLogCount()
        {
            Interlocked.Increment(ref _pushLogCount);
        }

        internal void DropLog(Int32 logCount)
        {
            Interlocked.Add(ref _failLogCount, logCount);
            Interlocked.Add(ref _pendingLogCount, -logCount);
        }

        internal void UpdateCounter(IDictionary<String, Int32> logTypeToCount, Int32 logCount, Int32 dropLogCount, Int32 sendBytes, Double elapsedTimeMS)
        {
            lock (_lock)
            {
                _temporalCounter.LogCount += logCount;
                _temporalCounter.DropLogCount += dropLogCount;
                _temporalCounter.SendBytes += sendBytes;
                _temporalCounter.ElapsedMSTotal += elapsedTimeMS;
                _temporalCounter.UpdateCount += 1;

                if (_temporalCounter.ElapsedMSPeak < elapsedTimeMS)
                {
                    _temporalCounter.ElapsedMSPeak = elapsedTimeMS;
                }
            }

            Interlocked.Add(ref _requestPutLogCount, logCount);
            Interlocked.Add(ref _failLogCount, dropLogCount);
            Interlocked.Add(ref _pendingLogCount, logCount);

            foreach (var pair in logTypeToCount)
            {
                LogTypeToCount.AddOrUpdate(pair.Key, pair.Value, (key, oldValue) => oldValue + pair.Value);
            }
        }

        internal void UpdateKinesisShard(ResponsePutRecords responsePutRecords)
        {
            lock (_lock)
            {
                foreach (Record record in responsePutRecords.Records)
                {
                    if (String.IsNullOrEmpty(record.ErrorCode) == false)
                    {
                        continue;
                    }

                    // 실패하면 ShardId가 null일 수 있다.
                    if (String.IsNullOrEmpty(record.ShardId) == false)
                    {
                        KinesisShardToCount.AddOrUpdate(record.ShardId, 1, (key, oldValue) => oldValue + 1);
                    }
                }
            }
        }

        internal void CompletePut(Int32 putLogCount)
        {
            Interlocked.Add(ref _successLogCount, putLogCount);
            Interlocked.Add(ref _pendingLogCount, -putLogCount);
        }

        internal void EnableForceReport()
        {
            lock (_lock)
            {
                _temporalCounter.ForceReportFlag = true;
            }
        }

        internal void ReportLogTypeToCount()
        {
            Dictionary<String, Int32> logTypeToCount;

            lock (_lock)
            {
                logTypeToCount = new Dictionary<String, Int32>(LogTypeToCount);
            }

            String reportText = JsonConvert.SerializeObject(logTypeToCount, Formatting.Indented);
            _logger.Reporter.Info($"LogType:Count\n```{reportText}```");
        }

        internal void ResetLogTypeToCount()
        {
            LogTypeToCount.Clear();
        }

        internal void ReportKinesisShardUsage()
        {
            Dictionary<String, Int32> kinesisShardToCount;

            lock (_lock)
            {
                kinesisShardToCount = new Dictionary<String, Int32>(KinesisShardToCount);
            }

            String reportText = JsonConvert.SerializeObject(kinesisShardToCount, Formatting.Indented);
            _logger.Reporter.Info($"ShardId:Count\n```{reportText}```");
        }

        private void RecordLogQueue()
        {
            lock (_lock)
            {
                Int32 queueCount = _logger.LogCountInQueue;
                _temporalCounter.QueueCounts.Add(queueCount);

                if (_temporalCounter.QueueCountPeak < queueCount)
                {
                    _temporalCounter.QueueCountPeak = queueCount;
                }
            }
        }

        private void Watching(DateTime now)
        {
            lock (_lock)
            {
                try
                {
                    WatchingImpl(now - _lastWatchingTime);
                }
                finally
                {
                    _lastWatchingTime = now;
                    _temporalCounter.Clear();
                }
            }
        }

        private void WatchingImpl(TimeSpan timeSpan)
        {
            if (timeSpan.Seconds <= 0)
            {
                return;
            }

            TemporalCounter t = _temporalCounter;

            Double logCountPerSec = t.LogCount / timeSpan.TotalSeconds;
            Double bytesPerSec = t.SendBytes / timeSpan.TotalSeconds;
            Double elapsedMSMean = t.UpdateCount <= 0 ? 0 : t.ElapsedMSTotal / t.UpdateCount;

            Statistics.MVS queueMVS = Statistics.CalculateMVS(t.QueueCounts);

            String attention = BuildSendSlackAttention(t.LogCount, t.ElapsedMSTotal, queueMVS);
            if (String.IsNullOrEmpty(attention) == false)
            {
                String message = String.Format("```{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}{15}{16}{17}{18}{19}{20}{21}{22}{23}{24}===========================================```",
                                               $"Attention: {attention}\n",
                                               $"Build: {_logger.BuildType}({_logger.Config.AssemblyVersion})\n",
                                               $"InstanceID: {_logger.InstanceID}\n",
                                               $"ID: {_logger.Config.ID}\n",
                                               $"StartTimeUTC(Elapsed): {_logger.StartTime:yyyy-MM-dd HH:mm:ss}({(DateTime.UtcNow - _logger.StartTime):dd\\.hh\\:mm\\:ss})\n",
                                               $"ReporterType(Level): {_logger.Config.ReporterType}({_logger.Reporter.ReportLevel})\n",
                                               $"ThreadCount: {_logger.ThreadCount.ToString("N0")}\n",
                                               $"PushLogCount: {_pushLogCount.ToString("N0")}\n",
                                               $"RequestPutLogCount: {_requestPutLogCount.ToString("N0")}\n",
                                               $"SuccessPutLogCount: {_successLogCount.ToString("N0")}\n",
                                               $"PendingPutLogCount: {_pendingLogCount.ToString("N0")}\n",
                                               $"FailLogCount: {_failLogCount.ToString("N0")}\n",
                                               $"SerialErrorCount: {_logger.ErrorCounter.SerialErrorCount.ToString("N0")}\n",
                                               $"TotalErrorCount: {_logger.ErrorCounter.TotalErrorCount.ToString("N0")}\n",
                                               $"UseIgnoreLogType: {_logger.UseIgnoreLogType.ToString()}\n",
                                               $"============ last {timeSpan.TotalSeconds.ToString("N2")} seconds =============\n",
                                               $"ElapsedPeak: {t.ElapsedMSPeak.ToString("N2")} ms\n",
                                               $"ElapsedMean: {elapsedMSMean.ToString("N2")} ms\n",
                                               $"WorkingCount: {t.UpdateCount.ToString("N0")}\n",
                                               $"PutLogCount: {t.LogCount.ToString("N0")}\n",
                                               $"SendBytes: {t.SendBytes.ToString("N0")}\n",
                                               $"QueueSizePeak: {t.QueueCountPeak.ToString("N0")}\n",
                                               $"QueueSizeMean(SD): {queueMVS.Mean.ToString("N2")}({queueMVS.SD.ToString("N2")})\n",
                                               $"{logCountPerSec.ToString("N2")} LogCount/s\n",
                                               $"{bytesPerSec.ToString("N2")} Bytes/s\n");

                _logger.Reporter.Info(message);
            }
        }

        private String BuildSendSlackAttention(Int32 logCount, Double elapsedMeanMS, Statistics.MVS queueMVS)
        {
            if (_temporalCounter.ForceReportFlag)
            {
                return "ForceReportFlag";
            }

            Config.Watcher config = _logger.Config.Watchers;

            if (config.ReportLogCount < logCount)
            {
                return $"LogCount({config.ReportLogCount.ToString()}) < LogCount({logCount.ToString()})";
            }

            if (config.ReportElapsedMeanMS < elapsedMeanMS)
            {
                return $"ElapsedMeanMS({config.ReportElapsedMeanMS.ToString()}) < ElapsedMS({elapsedMeanMS.ToString()})";
            }

            if (config.ReportQueueMeanSize < queueMVS.Mean)
            {
                return $"QueueSizeMean({config.ReportQueueMeanSize.ToString()}) < Mean({queueMVS.Mean.ToString()})";
            }

            if (config.ReportQueueStdDev < queueMVS.SD)
            {
                return $"QueueSizeMean({config.ReportQueueStdDev.ToString()}) < SD({queueMVS.SD.ToString()})";
            }

            if (_logger.BuildType == BuildType.Debug)
            {
                return "DEBUG";
            }

            return String.Empty; // 주목할 만한 것이 없는 상태이므로 메시지를 만들지 않는다.
        }
    }
}

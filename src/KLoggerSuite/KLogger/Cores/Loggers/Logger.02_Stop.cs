using System;
using System.Diagnostics;
using System.Threading;
using KLogger.Cores.Exceptions;
using KLogger.Libs;
using KLogger.Libs.Reporters;
using KLogger.Types;

namespace KLogger.Cores.Loggers
{
    /// <summary>
    ///     Logger의 정지.
    /// </summary>
    internal partial class Logger
    {
        public void Stop()
        {
            try
            {
                StopImpl();
            }
            catch (Exception exception)
            {
                Reporter.Fatal($"*[Exception] Fail Stop Logger!*\nInstanceID: `{InstanceID}`\nState: `{State}`\nException: {exception.Message}");
            }
        }

        private void StopImpl()
        {
            const Int32 WAIT_MS = 30000;

            lock (_lock)
            {
                if ((State == StateType.Start || State == StateType.Pause) == false)
                {
                    Reporter.Warn($"*[Invalid State] Fail Stop Logger!*\nInstanceID: `{InstanceID}`\nState: `{State}`");
                    return;
                }

                State = StateType.Stopping;

                StopWatcher();
                StopLoggerThread();

                WaitForFlushLogQueue(WAIT_MS);

                StopCompletePutHandler();    // 전송 실패에 대한 처리는 큐의 플러시 이후에 정지해야 한다.

                StopThroughputController();

                State = StateType.Stop;
            }

            Reporter.Fatal($"*Stop Logger!*\nInstanceID: `{InstanceID}`\nPendingLog: `{Watcher.PendingLogCount.ToString()}`");
            WaitForRemainReport();
        }

        private void StopWatcher()
        {
            Watcher.Stop();
        }

        private void StopLoggerThread()
        {
            foreach (NaiveLoopThread loggerThread in _loggerThreads)
            {
                loggerThread.Stop();
            }

            _loggerThreads.Clear();
        }

        private void WaitForFlushLogQueue(Int32 waitMS)
        {
            if (State == StateType.Start)
            {
                throw new LoggerException($"{nameof(WaitForFlushLogQueue)} Invalid Status: {State}");
            }

            while (_logQueue.IsEmpty() == false)
            {
                DebugLog.Log($"Flush Remain Log({_logQueue.Count.ToString()})", "klogger:stop");

                TickForced();
            }

            var sw = new Stopwatch();
            sw.Start();

            while (0 < Watcher.PendingLogCount)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(10));

                if (waitMS < sw.ElapsedMilliseconds)
                {
                    break;
                }
            }

            sw.Stop();

            if (0 < Watcher.PendingLogCount)
            {
                Reporter.Fatal($"*Maybe Loss Log! - Timeout Stop*\nCount: `{Watcher.PendingLogCount.ToString()}`");
            }
        }

        private void StopCompletePutHandler()
        {
            CompletePutNotifier?.Stop();
        }

        private void StopThroughputController()
        {
            ThroughputController?.Stop();
        }

        private void WaitForRemainReport()
        {
            if (Reporter is SlackReporter reporter)
            {
                const Int32 WAIT_MS = 5000;
                reporter.CleanupAndWaitForAsyncSend(WAIT_MS);
            }
        }
    }
}

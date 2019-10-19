using System;
using System.Collections.Generic;
using System.Threading;
using KLogger.Configs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class DropLogTests : KLoggerTests
    {
        protected override CompletePutNotifyType CompletePutNotifyType => CompletePutNotifyType.Both;

        protected override void OnCompletePut(IEnumerable<CompletePut> completePuts)
        {
            foreach (CompletePut completePut in completePuts)
            {
                Console.WriteLine($"{nameof(OnCompletePut)}: {completePut.Logs.Length}, {completePut.CompletePutType}");

                if (completePut.CompletePutType == CompletePutType.FailRetry)
                {
                    Console.WriteLine("올바르지 않은 결과");
                }

                Assert.IsTrue(completePut.CompletePutType == CompletePutType.FailRetry);

                foreach (ILog log in completePut.Logs)
                {
                    if (_sequenceToLogObject.TryRemove(log.Sequence, out Object originLogObject) == false)
                    {
                        Console.WriteLine("보낸 목록에서 찾을 수 없는 로그");
                        Assert.Fail("보낸 목록에서 찾을 수 없는 로그");
                    }

                    Object logObject = log.LogObject;
                    if (log.LogObject is StringLogObject logLogObject)
                    {
                        logObject = logLogObject.raw;
                    }

                    if (logObject != originLogObject)
                    {
                        Console.WriteLine("보냈던 로그와 다른 로그");
                        Assert.Fail("보냈던 로그와 다른 로그");
                    }
                }
            }

            if (_sequenceToLogObject.IsEmpty)
            {
                _completeTestEvent.Set();
            }
        }

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();
            
            // 이 테스트는 슬랙에서 보자.
            _kLoggerConfig.ReporterType = ReporterType.Slack;

            // 원래 Config에 의해 1MB 넘는 것은 전송할 수 없지만 강제로 해제한다.
            // 해제하지 않으면 실제 전송까지 가지 못하고 인코딩에서 TooLargeLogSize 통지가 온다.
            _kLoggerConfig.MaxRecordByte = Const.MAX_KINESIS_RECORD_BYTE * 10;

            // 압축하지 않도록 한다.
            _kLoggerConfig.CompressLogThresholdByte = _kLoggerConfig.MaxRecordByte + 1;

            // 테스트 시간을 줄이기 위해 재시도 횟수를 조절한다.
            _kLoggerConfig.MaxRetrySendCount = 2;

            // 테스트 시간을 줄이기 위해 로그 모으는 시간을 조절한다.
            _kLoggerConfig.MaxBatchWaitTimeMS = 1000;

            return config;
        }

        [TestMethod]
        public void 재시도_횟수를_초과하여_로그를_버리는_테스트()
        {
            const Int32 LOG_COUNT = 1;

            // 모든 재전송 횟수를 소모하는 것까지 고려해서 넉넉히 잡는다.
            const Int32 MAX_WAIT_TEST_MS = 1000 * 60 * 2;

            // Kinesis는 레코드가 1MB가 넘으면 안되는 것을 이용해서 재시도 테스트를 한다.
            String text = System.IO.File.ReadAllText(@"../../nietzsche.txt");   // 약 600k
            text += text;
            text += text;   // 이제 약 1.8MB

            AddLogIfFailAssert(text);

            Boolean isTimeout = _completeTestEvent.Wait(MAX_WAIT_TEST_MS);
            Assert.IsTrue(isTimeout);

            WaitForCompleteTest(MAX_WAIT_TEST_MS, 0, LOG_COUNT);

            에러_카운트_테스트();

            // 종료하기 전에 현재 상황 출력.
            _kLoggerAPI.ReportStatus();
            
            Thread.Sleep(_kLoggerConfig.Watchers.IntervalMS);   // watcher가 한 번 돌기를 기다린다.
        }

        private void 에러_카운트_테스트()
        {
            Int32 errorCount = _kLoggerConfig.MaxRetrySendCount + 1;

            Assert.IsTrue(_kLoggerAPI.SerialErrorCount == errorCount);
            Assert.IsTrue(_kLoggerAPI.TotalErrorCount == errorCount);

            _kLoggerAPI.ResetError();

            Assert.IsTrue(_kLoggerAPI.SerialErrorCount == 0);
            Assert.IsTrue(_kLoggerAPI.TotalErrorCount  == 0);
        }
    }
}

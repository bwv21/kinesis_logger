using System;
using System.Collections.Generic;
using KLogger.Configs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class FailCompletePutNoticeTests : KLoggerTests
    {
        protected override CompletePutNotifyType CompletePutNotifyType => CompletePutNotifyType.Both;

        protected override void OnCompletePut(IEnumerable<CompletePut> completePuts)
        {
            foreach (CompletePut completePut in completePuts)
            {
                Console.WriteLine($"{nameof(OnCompletePut)}: {completePut.Logs.Length}");

                Assert.IsTrue(completePut.CompletePutType == CompletePutType.TooLargeLogSize);
                
                foreach (ILog log in completePut.Logs)
                {
                    if (_sequenceToLogObject.TryRemove(log.Sequence, out Object originLogObject) == false)
                    {
                        Assert.Fail("보낸 목록에서 찾을 수 없는 로그");
                    }

                    Object logObject = log.LogObject;
                    if (log.LogObject is StringLogObject logLogObject)
                    {
                        logObject = logLogObject.raw;
                    }

                    if (logObject != originLogObject)
                    {
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

            // 테스트 시간을 줄이기 위해 재시도 횟수를 조절한다.
            _kLoggerConfig.MaxRetrySendCount = 2;

            // 테스트 시간을 줄이기 위해 로그 모으는 시간을 조절한다.
            _kLoggerConfig.MaxBatchWaitTimeMS = 1000;

            return config;
        }

        [TestMethod]
        public void 최대_로그_크기를_초과하여_실패_통지_테스트()
        {
            const Int32 MAX_WAIT_TEST_MS = 1000 * 10;

            // Kinesis는 레코드가 1MB 가 넘으면 안되는 것을 이용해서 재시도 테스트를 한다.
            String text = System.IO.File.ReadAllText(@"../../nietzsche.txt"); // 약 600k
            text += text;
            text += text;

            AddLogIfFailAssert(text);

            WaitForCompleteTest(MAX_WAIT_TEST_MS, 0, 1);
        }
    }
}

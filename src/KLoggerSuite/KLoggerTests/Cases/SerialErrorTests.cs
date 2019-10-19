using System;
using System.Collections.Generic;
using KLogger.Configs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class SerialErrorTests : KLoggerTests
    {
        private const Int32 SERIAL_ERROR_COUNT = 3;

        protected override CompletePutNotifyType CompletePutNotifyType => CompletePutNotifyType.FailOnly;

        protected override void OnCompletePut(IEnumerable<CompletePut> completePuts)
        {
            foreach (CompletePut completePut in completePuts)
            {
                Console.WriteLine($"{nameof(OnCompletePut)}: {completePut.Logs.Length}");

                Assert.IsTrue(completePut.CompletePutType == CompletePutType.FailRetry);

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

            // 있을 수 없는 키네시스 스트림이름을 사용해서 전송이 불가능한 상태로 만든다.
            // 계속해서 에러가 발생할 것이다.
            config.AWSs.KinesisStreamName = "=====================";

            config.MaxSerialErrorCount = SERIAL_ERROR_COUNT;
            config.MaxRetrySendCount = SERIAL_ERROR_COUNT;

            // 테스트 시간을 줄이기 위해 로그 모으는 시간을 조절한다.
            _kLoggerConfig.MaxBatchWaitTimeMS = 500;

            return config;
        }

        [TestMethod]
        public void 연속_에러가_발생했을_때_로거가_멈추는_테스트()
        {
            const Int32 MAX_WAIT_TEST_MS = 1000 * 60;

            AddLogIfFailAssert("fail-log", "test-type");

            WaitForCompleteTest(MAX_WAIT_TEST_MS, 0, 1);

            Assert.IsTrue(_kLoggerAPI.State == StateType.Pause);

            // 재시도를 하면서 추가로 에러가 발생할 수 있으므로 에러가 더 많을 수 있다.
            Assert.IsTrue(SERIAL_ERROR_COUNT <= _kLoggerAPI.SerialErrorCount);
        }
    }
}

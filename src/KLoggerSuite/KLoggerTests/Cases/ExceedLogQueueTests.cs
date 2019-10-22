using System;
using KLogger.Configs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class ExceedLogQueueTests : KLoggerTests
    {
        private const Int32 MAX_LOG_QUEUE_SIZE = 3;

        protected override CompletePutNoticeType CompletePutNoticeType => CompletePutNoticeType.Both;

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();

            // 너무 빠르면 큐가 비워질 수 있으니 약간 크게한다.
            _kLoggerConfig.MaxBatchWaitTimeMS = 5000;

            _kLoggerConfig.MaxLogQueueSize = MAX_LOG_QUEUE_SIZE;

            return config;
        }

        [TestMethod]
        public void 로그_큐가_꽉차서_푸시에_실패하는_테스트()
        {
            const Int32 MAX_WAIT_TEST_MS = 1000 * 10;

            Object log = new { Val = 123 };

            for (Int32 i = 0; i < MAX_LOG_QUEUE_SIZE; ++i)
            {
                AddLogIfFailAssert(log);
            }
            
            Int64 sequence = _kLoggerAPI.Push("test", log);

            Assert.IsTrue(sequence < 0);

            WaitForCompleteTest(MAX_WAIT_TEST_MS, MAX_LOG_QUEUE_SIZE, 0);
        }
    }
}

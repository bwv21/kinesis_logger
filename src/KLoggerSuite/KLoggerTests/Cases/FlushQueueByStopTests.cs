using System;
using KLogger.Configs;
using KLogger.Libs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class FlushQueueByStopTests : KLoggerTests
    {
        protected override CompletePutNotifyType CompletePutNotifyType => CompletePutNotifyType.Both;

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();

            // 큐를 비우지 않도록 모으는 시간을 길게한다.
            _kLoggerConfig.MaxBatchWaitTimeMS = 1000 * 60 * 60;

            return config;
        }

        [TestMethod]
        public void 로그를_푸시하고_바로_종료하여_큐를_플러시하는_테스트()
        {
            const Int32 LOG_COUNT = 5;

            // 전송이 완료될 때까지 적당히 기다린다(정확한 시간을 정할수 없다).
            const Int32 MAX_WAIT_TEST_MS = 1000 * 30;

            for (Int32 i = 0; i < LOG_COUNT; ++i)
            {
                Object logObject = new
                                   {
                                       ValueInt = i,
                                       ValueString = Rand.RandString(Rand.RandInt32(1, 5))
                                   };

                AddLogIfFailAssert(logObject);
            }

            // 여기서 큐를 플러시 한다.
            _kLoggerAPI.Stop();

            WaitForCompleteTest(MAX_WAIT_TEST_MS, LOG_COUNT, 0);
        }
    }
}

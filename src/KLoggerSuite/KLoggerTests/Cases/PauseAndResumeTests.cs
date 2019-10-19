using System;
using KLogger.Configs;
using KLogger.Libs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class PauseAndResumeTests : KLoggerTests
    {
        protected override CompletePutNotifyType CompletePutNotifyType => CompletePutNotifyType.Both;

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();

            // 테스트 시간을 줄이기 위해 로그 모으는 시간을 조절한다.
            _kLoggerConfig.MaxBatchWaitTimeMS = 500;

            return config;
        }

        [TestMethod]
        public void Pause상태_변경_후_푸시_실패_Resume상태_변경_후_푸시_성공_테스트()
        {
            const Int32 LOG_COUNT = 2;

            // 전송이 완료될 때까지 적당히 기다린다(정확한 시간을 정할수 없다).
            const Int32 MAX_WAIT_TEST_MS = 1000 * 30;

            Object logObject = new
                               {
                                   ValueInt = 0,
                                   ValueString = Rand.RandString(Rand.RandInt32(0, 32))
                               };

            AddLogIfFailAssert(logObject);

            _kLoggerAPI.Pause();
            Assert.ThrowsException<AssertFailedException>(() => AddLogIfFailAssert(logObject));

            _kLoggerAPI.Resume();
            AddLogIfFailAssert(logObject);

            WaitForCompleteTest(MAX_WAIT_TEST_MS, LOG_COUNT, 0);
        }
    }
}

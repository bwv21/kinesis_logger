using System;
using KLogger.Configs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class ReportTests : KLoggerTests
    {
        protected override CompletePutNotifyType CompletePutNotifyType => CompletePutNotifyType.Both;

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();

            // 테스트 시간을 줄이기 위해 로그 모으는 시간을 조절한다.
            _kLoggerConfig.MaxBatchWaitTimeMS = 1000;

            config.ReporterType = ReporterType.Slack;

            return config;
        }

        // 슬랙에서 눈으로 확인하는 것으로 타협한다.
        [TestMethod]
        public void 리포트_테스트()
        {
            const Int32 TEST_COUNT = 10;
            const Int32 MAX_WAIT_TEST_MS = 1000 * 30;

            // 임시로 슬랙으로 보내고 눈으로 확인 한다.
            for (Int32 i = 0; i < TEST_COUNT; ++i)
            {
                AddLogIfFailAssert($"test: {i}");
            }

            WaitForCompleteTest(MAX_WAIT_TEST_MS, TEST_COUNT, 0);

            _kLoggerAPI.ReportConfig();
            _kLoggerAPI.ReportLogTypeToCount();
            _kLoggerAPI.ReportKinesisShardUsage();

            _kLoggerAPI.ResetLogTypeToCount();
        }
    }
}

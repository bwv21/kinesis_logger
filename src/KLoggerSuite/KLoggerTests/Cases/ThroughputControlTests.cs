using System;
using KLogger.Configs;
using KLogger.Libs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class ThroughputControlTests : KLoggerTests
    {
        protected override CompletePutNoticeType CompletePutNoticeType => CompletePutNoticeType.Both;

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();

            _kLoggerConfig.UseThroughputControl = 1;
            
            // 테스트 편의를 위해 제한을 푼다.
            _kLoggerConfig.MaxLogQueueSize = Int32.MaxValue;

            // 이 테스트는 슬랙에서 보자.
            _kLoggerConfig.ReporterType = ReporterType.Slack;

            return config;
        }

        [TestMethod]
        public void 스루풋_조절_바이트_테스트()
        {
            const Int32 LOG_COUNT = 3000;
            const Int32 MIN_LOG_STRING = 10;
            const Int32 MAX_LOG_STRING = 10000;

            // 재전송까지 감안해서 적당히 기다린다.
            const Int32 MAX_WAIT_TEST_MS = 1000 * 30;

            for (Int32 i = 0; i < LOG_COUNT; ++i)
            {
                Object logObject = new
                                   {
                                       ValueInt = i,
                                       ValueString = Rand.RandString(Rand.RandInt32(MIN_LOG_STRING, MAX_LOG_STRING))
                                   };

                AddLogIfFailAssert(logObject);
            }

            WaitForCompleteTest(MAX_WAIT_TEST_MS, LOG_COUNT, 0);
        }

        // 필요할 때만 주석 풀어서 테스트.
        //[TestMethod]
        public void 스루풋_조절_레코드_테스트()
        {
            const Int32 LOG_COUNT = 10000;
            const Int32 MIN_LOG_STRING = 1;
            const Int32 MAX_LOG_STRING = 20;

            // 재전송까지 감안해서 적당히 기다린다.
            const Int32 MAX_WAIT_TEST_MS = 1000 * 60;

            for (Int32 i = 0; i < LOG_COUNT; ++i)
            {
                Object logObject = new
                                   {
                                       ValueInt = i,
                                       ValueString = Rand.RandString(Rand.RandInt32(MIN_LOG_STRING, MAX_LOG_STRING))
                                   };

                AddLogIfFailAssert(logObject);
            }

            WaitForCompleteTest(MAX_WAIT_TEST_MS, LOG_COUNT, 0);
        }
    }
}

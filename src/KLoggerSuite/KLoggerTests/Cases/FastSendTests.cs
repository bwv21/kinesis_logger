using System;
using KLogger.Configs;
using KLogger.Libs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class FastSendTests : KLoggerTests
    {
        protected override CompletePutNoticeType CompletePutNoticeType => CompletePutNoticeType.Both;

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();

            // 스루풋 제어를 꺼야 용량초과가 잘 일어난다.
            _kLoggerConfig.UseThroughputControl = 0;
            
            return config;
        }

        [TestMethod]
        public void 큰_로그를_빠르게_보내서_용량초과로_재전송이_일어나는_테스트()
        {
            // 샤드 1개 기준으로 350개 정도만 들어가고 나머지는 실패하여 재시도하게 된다.
            const Int32 LOG_COUNT = 500;
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
    }
}

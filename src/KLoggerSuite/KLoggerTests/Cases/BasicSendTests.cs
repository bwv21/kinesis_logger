using System;
using System.Threading;
using KLogger.Configs;
using KLogger.Libs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class BasicSendTests : KLoggerTests
    {
        protected override CompletePutNoticeType CompletePutNoticeType => CompletePutNoticeType.Both;

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();

            // 테스트 시간을 줄이기 위해 로그 모으는 시간을 조절한다.
            _kLoggerConfig.MaxBatchWaitTimeMS = 100;

            return config;
        }

        [TestMethod]
        public void 기본_테스트()
        {
            const Int32 LOG_COUNT = 30;
            const Int32 MIN_LOG_STRING = 1;
            const Int32 MAX_LOG_STRING = 1200;
            const Int32 MIN_SLEEP_MS = 10;
            const Int32 MAX_SLEEP_MS = 30;

            // 전송이 완료될 때까지 적당히 기다린다(정확한 시간을 정할수 없다).
            const Int32 MAX_WAIT_TEST_MS = 1000 * 30;

            for (Int32 i = 0; i < LOG_COUNT; ++i)
            {
                if (i % 2 == 0)
                {
                    // 익명 객체.
                    Object logObject = new
                                       {
                                           ValueInt = i,
                                           ValueString = Rand.RandString(Rand.RandInt32(MIN_LOG_STRING, MAX_LOG_STRING))
                                       };

                    AddLogIfFailAssert(logObject);
                }
                else if (i % 3 == 0)
                {
                    // json 문자열.
                    String jsonStr = JsonConvert.SerializeObject(new
                                                                 {
                                                                     ValueInt = i,
                                                                     ValueString = Rand.RandString(Rand.RandInt32(MIN_LOG_STRING, MAX_LOG_STRING))
                                                                 });
                    AddLogIfFailAssert(jsonStr);
                }
                else
                {
                    // 일반 문자열.
                    String str = Rand.RandString(100);
                    AddLogIfFailAssert(str);
                }

                Thread.Sleep(Rand.RandInt32(MIN_SLEEP_MS, MAX_SLEEP_MS));
            }

            WaitForCompleteTest(MAX_WAIT_TEST_MS, LOG_COUNT, 0);
        }
    }
}

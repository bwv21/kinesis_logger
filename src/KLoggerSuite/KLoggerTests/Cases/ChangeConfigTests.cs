using System;
using System.Linq;
using KLogger.Configs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class ChangeConfigTests : KLoggerTests
    {
        private const String IGNORE_TYPE = "ignore_test_type";

        protected override CompletePutNotifyType CompletePutNotifyType => CompletePutNotifyType.Both;

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();

            // 테스트 시간을 줄이기 위해 로그 모으는 시간을 조절한다.
            _kLoggerConfig.MaxBatchWaitTimeMS = 1000;

            config.UseIgnoreLogType = 0;
            config.IgnoreLogTypes.Add(IGNORE_TYPE);

            return config;
        }

        [TestMethod]
        public void 설정_변경_로거_API_테스트()
        {
            const Int32 LOG_COUNT = 2;
            const Int32 MAX_WAIT_TEST_MS = 1000 * 30;

            _kLoggerAPI.EnableIgnoreLog(true);

            Int64 sequence = _kLoggerAPI.Push(IGNORE_TYPE, "...");
            Assert.IsTrue(sequence < 0);

            _kLoggerAPI.EnableIgnoreLog(false);

            AddLogIfFailAssert("...", IGNORE_TYPE);

            _kLoggerAPI.EnableIgnoreLog(true);

            sequence = _kLoggerAPI.Push(IGNORE_TYPE, "...");
            Assert.IsTrue(sequence < 0);

            _kLoggerAPI.RemoveIgnoreLogType(IGNORE_TYPE);

            AddLogIfFailAssert("...", IGNORE_TYPE);

            _kLoggerAPI.AddIgnoreLogType(IGNORE_TYPE);

            sequence = _kLoggerAPI.Push(IGNORE_TYPE, "...");
            Assert.IsTrue(sequence < 0);

            var ignoreLogTypes = _kLoggerAPI.IgnoreLogTypes;
            Assert.IsTrue(ignoreLogTypes.ElementAt(0) == IGNORE_TYPE);

            WaitForCompleteTest(MAX_WAIT_TEST_MS, LOG_COUNT, 0);
        }
    }
}
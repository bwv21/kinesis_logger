using System;
using KLogger.Configs;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class PushFailTests : KLoggerTests
    {
        private const String IGNORE_TYPE = "ignore_test_type";

        protected override CompletePutNoticeType CompletePutNoticeType => CompletePutNoticeType.Both;

        protected override Config OverwriteConfig()
        {
            // config를 이렇게 바꿀 수 있는 건 friend-assembly이기 때문이다.
            Config config = base.OverwriteConfig();

            config.UseIgnoreLogType = 1;
            config.IgnoreLogTypes.Add(IGNORE_TYPE);

            return config;
        }

        [TestMethod]
        public void 푸시_실패_테스트()
        {
            // null test.
            Int64 sequence = _kLoggerAPI.Push("null_test", null);
            Assert.IsTrue(sequence < 0);
            sequence = _kLoggerAPI.Push(null, "...");
            Assert.IsTrue(sequence < 0);
            sequence = _kLoggerAPI.Push(null, null);
            Assert.IsTrue(sequence < 0);

            // JSON이 아닌데 PushJsonString 함수 사용.
            sequence = _kLoggerAPI.PushJsonString("fail_test", "}}}}{{{{");
            Assert.IsTrue(sequence < 0);

            // 무시 타입으로 푸시.
            sequence = _kLoggerAPI.Push(IGNORE_TYPE, "...");
            Assert.IsTrue(sequence < 0);
        }
    }
}

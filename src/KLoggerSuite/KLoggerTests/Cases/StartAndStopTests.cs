using System.Threading;
using KLogger.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Cases
{
    [TestClass]
    public class StartAndStopTests : KLoggerTests
    {
        protected override CompletePutNotifyType CompletePutNotifyType => CompletePutNotifyType.None;

        [TestMethod]
        public void 여러_스레드에서_시작과_종료를_수행()
        {
            // 단일 스레드에서 시작과 정지를 번갈아가며 테스트.
            // 이미 시작한 상태이므로 정지부터 한다.
            _kLoggerAPI.Stop();
            _kLoggerAPI.StartIfFailThrow();
            _kLoggerAPI.Stop();
            _kLoggerAPI.StartIfFailThrow();
            _kLoggerAPI.Stop();
            // . //

            // 여러 스레드에서 시작과 정지를 `올바르지 않게` 사용. //
            var thread1 = new Thread(() =>
                                     {
                                         _kLoggerAPI.Start();
                                         _kLoggerAPI.Stop();
                                         _kLoggerAPI.Start();
                                         _kLoggerAPI.Stop();
                                     });

            var thread2 = new Thread(() =>
                                     {
                                         _kLoggerAPI.Start();
                                         _kLoggerAPI.Stop();
                                         _kLoggerAPI.Start();
                                         _kLoggerAPI.Stop();
                                     });

            var thread3 = new Thread(() =>
                                     {
                                         _kLoggerAPI.Start();
                                         _kLoggerAPI.Stop();
                                         _kLoggerAPI.Start();
                                         _kLoggerAPI.Stop();
                                     });

            thread1.Start();
            thread2.Start();
            thread3.Start();
            thread1.Join();
            thread2.Join();
            thread3.Join();

            _kLoggerAPI.Stop(); // 스레드 테스트가 끝나고 시작 상태면 정지.
            // . //

            // 다시 단일 스레드에서 시작과 정지를 번갈아가며 테스트.
            _kLoggerAPI.StartIfFailThrow();
            _kLoggerAPI.Stop();
            _kLoggerAPI.StartIfFailThrow();
            _kLoggerAPI.Stop();
            _kLoggerAPI.StartIfFailThrow();
            _kLoggerAPI.Stop();
            //. //

            Assert.IsTrue(_kLoggerAPI.State == StateType.Stop);

            // 시작 상태로 되돌린다.
            _kLoggerAPI.StartIfFailThrow();
        }
    }
}

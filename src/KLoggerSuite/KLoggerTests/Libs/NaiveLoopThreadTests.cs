using System;
using System.Threading;
using KLogger.Libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KLoggerTests.Libs
{
    [TestClass]
    public class NaiveLoopThreadTests
    {
        private readonly Object _lock = new Object();

        private NaiveLoopThread _naiveLoopThread;
        private Int32 _loopCount;

        [TestMethod]
        public void 데드락_유발_사용하는_쪽에서_Loop와_Stop에_락()
        {
            _naiveLoopThread = new NaiveLoopThread(Loop_1, 1000, Console.WriteLine, nameof(데드락_유발_사용하는_쪽에서_Loop와_Stop에_락));
            _naiveLoopThread.Start();
            Stop_1();

            while (_loopCount <= 0)
            {
                Console.WriteLine("wait for test...");
                Thread.Sleep(500);
            }

            Console.WriteLine("test ok.");
        }

        private void Loop_1()
        {
            Thread.Sleep(1000); // Stop이 락을 먼저 잡게 된다.

            Console.WriteLine("loop try get lock");

            lock (_lock)
            {
                Console.WriteLine("loop get lock");
            }

            Console.WriteLine("loop release lock");

            Interlocked.Increment(ref _loopCount);
        }

        private void Stop_1()
        {
            Console.WriteLine("stop try get lock");

            lock (_lock)
            {
                Console.WriteLine("stop get lock");

                // Stop이 락을 먼저 잡았으니 loop가 락을 대기한다.
                // Stop을 잘못 구현해서 loop를 대기하게 되면 loop는 Stop의 락이 풀리기를 기다리므로 데드락이 발생한다.
                _naiveLoopThread.Stop();
            }

            Console.WriteLine("stop release lock");
        }

        [TestMethod]
        public void 데드락_유발_루프_안에서_Stop호출()
        {
            _naiveLoopThread = new NaiveLoopThread(Loop_2, 1000, Console.WriteLine, nameof(데드락_유발_루프_안에서_Stop호출));
            _naiveLoopThread.Start();

            while (_loopCount <= 0)
            {
                Console.WriteLine("wait for test...");
                Thread.Sleep(500);
            }

            Console.WriteLine("test ok.");
        }

        private void Loop_2()
        {
            // 여긴 NaiveLoopThread 안쪽인데 Stop을 호출한다.
            _naiveLoopThread.Stop();

            Interlocked.Increment(ref _loopCount);
        }
    }
}

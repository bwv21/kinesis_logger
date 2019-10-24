using System;
using System.Diagnostics;
using System.Threading;

namespace KLogger.Libs
{
    internal class NaiveLoopThread
    {
        private readonly Object _lock = new Object();

        private readonly Action _loop;
        private readonly Int32 _loopIntervalMS;
        private readonly Action<String> _onError;
        private readonly String _name;

        private volatile Thread _thread;

        private ManualResetEvent _threadStartEvent;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private Stopwatch _stopwatch;

        public NaiveLoopThread(Action loop, Int32 loopIntervalMS, Action<String> onError, String name = "NoName")
        {
            _loop = loop;
            _loopIntervalMS = loopIntervalMS;
            _onError = onError;
            _name = name;
        }

        ~NaiveLoopThread()
        {
            if (_cancellationToken.IsCancellationRequested == false)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();

                _onError?.Invoke($"{nameof(NaiveLoopThread)} Missing Stop!");
            }
        }

        public void Start(Boolean isBackground = false)
        {
            lock (_lock)
            {
                if (_thread != null)
                {
                    throw new Exception($"{_name}: Fail Start Thread - previous thread is running!");
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _cancellationToken = _cancellationTokenSource.Token;
                _stopwatch = new Stopwatch();
                _threadStartEvent = new ManualResetEvent(false);
                
                _thread = new Thread(Loop) { IsBackground = isBackground }; // C# 기본값은 false(true로 하면 메인스레드 종료시 같이 종료된다).
                _thread.Start();

                _threadStartEvent.WaitOne();
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                if (_thread == null)
                {
                    throw new Exception($"{_name}: Fail Stop Thread - thread is not started!");
                }

                if (Thread.CurrentThread.ManagedThreadId == _thread.ManagedThreadId)
                {
                    _onError?.Invoke($"{nameof(NaiveLoopThread)} Warning! MainLoop Call Stop Function() !!!");
                }

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    _onError?.Invoke($"{nameof(NaiveLoopThread)} Warning! Already Called Stop Function() !!!");
                    return;
                }

                _cancellationTokenSource.Cancel();

                // 이곳에서 Join이나 Event를 대기하면 데드락에 빠질 수 있다(락 밖이라도).
                // 1. Loop(_loop) 에서 Stop을 부르는 경우, Loop()가 끝나지 않기 때문에 무한대기하게 된다.
                // 2. 사용하는 측에서 Loop(_loop)/Stop을 lock으로 묶은 경우, Stop에서 _loop락을 먼저 잡고 _loop가 끝나기를 대기하게 될 수 있다.
            }
        }

        private void Loop()
        {
            const Int32 MAX_WHEN_EXCEPTION_ADD_SLEEP_MS = 1000;

            DebugLog.Log($"{_name} {nameof(NaiveLoopThread)} BEGIN", "klogger:thread");

            _threadStartEvent.Set();

            Boolean cancelled = false;
            Int32 serialExceptionCount = 0;

            Int32 sleepMS = _loopIntervalMS;
            while (cancelled == false)
            {
                try
                {
                    cancelled = _cancellationToken.WaitHandle.WaitOne(TimeSpan.FromMilliseconds(sleepMS));

                    _stopwatch.Restart();

                    _loop();

                    _stopwatch.Stop();

                    serialExceptionCount = 0;

                    sleepMS = Math.Max(0, (Int32)(_loopIntervalMS - _stopwatch.ElapsedMilliseconds));
                }
                catch (Exception exception)
                {
                    ++serialExceptionCount;

                    sleepMS += Math.Min(MAX_WHEN_EXCEPTION_ADD_SLEEP_MS, _loopIntervalMS * (serialExceptionCount + 1));

                    _onError?.Invoke($"{exception.Message}\n{exception.StackTrace}");
                }
            }

            _cancellationTokenSource.Dispose();
            _thread = null;

            DebugLog.Log($"{_name} {nameof(NaiveLoopThread)} END", "klogger:thread");
        }
    }
}

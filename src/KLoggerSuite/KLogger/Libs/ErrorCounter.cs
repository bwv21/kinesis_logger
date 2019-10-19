using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using KLogger.Libs.Reporters;

namespace KLogger.Libs
{
    internal class ErrorCounter
    {
        private readonly Reporter _reporter;
        private readonly Action _onMaxErrorAction;
        private readonly Int32 _maxSerialErrorCount;

        private Int32 _totalErrorCount;
        private Int32 _serialErrorCount;

        public Int32 TotalErrorCount => _totalErrorCount;
        public Int32 SerialErrorCount => _serialErrorCount;

        public ErrorCounter(Reporter reporter, Int32 maxSerialErrorCount, Action onMaxErrorAction)
        {
            _reporter = reporter;
            _maxSerialErrorCount = maxSerialErrorCount;
            _onMaxErrorAction = onMaxErrorAction;
            _totalErrorCount = 0;
            _serialErrorCount = 0;
        }

        public void RaiseError(String message, Boolean report = true, [CallerFilePath]String sourceFilePath = "", [CallerLineNumber]Int32 sourceLineNumber = 0)
        {
            Interlocked.Increment(ref _serialErrorCount);
            Interlocked.Increment(ref _totalErrorCount);

            if (report)
            {
                _reporter?.Error($"[Error] {message} `{Path.GetFileName(sourceFilePath)}:{ sourceLineNumber.ToString()}` (`{_serialErrorCount}/{_totalErrorCount}`)");
            }

            DebugLog.Log($"{message} ({Path.GetFileName(sourceFilePath)}:{ sourceLineNumber.ToString()}) - {_serialErrorCount}/{_totalErrorCount}");

            if (_maxSerialErrorCount <= _serialErrorCount)
            {
                _reporter?.Fatal($"[Error] MaxError({_maxSerialErrorCount.ToString()}) <= SerialError({_serialErrorCount.ToString()})");

                if (_maxSerialErrorCount == _serialErrorCount)
                {
                    _onMaxErrorAction?.Invoke();
                }
            }
        }

        public void ResetAllError()
        {
            Interlocked.Exchange(ref _totalErrorCount, 0);
            Interlocked.Exchange(ref _serialErrorCount, 0);
        }

        public void ResetSerialError()
        {
            Interlocked.Exchange(ref _serialErrorCount, 0);
        }
    }
}

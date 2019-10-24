using System;

namespace KLogger.Cores.Exceptions
{
    /// <summary> 로거의 예외. </summary>
    public class LoggerException : Exception
    {
        /// <summary> 문자열을 받는 생성자. </summary>
        public LoggerException(String message)
            : base(message)
        {
        }
    }
}

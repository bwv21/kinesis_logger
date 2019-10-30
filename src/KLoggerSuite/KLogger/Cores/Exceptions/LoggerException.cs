using System;

namespace KLogger.Cores.Exceptions
{
    /// <summary> 로거의 예외. </summary>
    internal class LoggerException : Exception
    {
        /// <summary>
        /// 문자열을 받는 생성자. 
        /// </summary>
        /// <param name="message">
        /// 예외 메시지.
        /// </param>
        public LoggerException(String message)
            : base(message)
        {
        }
    }
}

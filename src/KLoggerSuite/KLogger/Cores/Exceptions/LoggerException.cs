using System;

namespace KLogger.Cores.Exceptions
{
    public class LoggerException : Exception
    {
        public LoggerException(String message)
            : base(message)
        {
        }
    }
}

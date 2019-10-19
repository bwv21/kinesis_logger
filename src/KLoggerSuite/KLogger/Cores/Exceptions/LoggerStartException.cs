using KLogger.Types;

namespace KLogger.Cores.Exceptions
{
    public class LoggerStartException : LoggerException
    {
        public StartResultType StartResult;

        public LoggerStartException(StartResultType startResult)
            : base(startResult.ToString())
        {
            StartResult = startResult;
        }
    }
}

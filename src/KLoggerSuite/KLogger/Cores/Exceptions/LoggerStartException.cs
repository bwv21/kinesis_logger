using KLogger.Cores.Loggers;
using KLogger.Types;

namespace KLogger.Cores.Exceptions
{
    /// <summary> 로거 시작(<see cref="Logger.Start"/>)의 예외. </summary>
    internal class LoggerStartException : LoggerException
    {
        /// <summary> 로거 시작(<see cref="Logger.Start"/>)의 실패 결과. </summary>
        public StartResultType StartResult;

        internal LoggerStartException(StartResultType startResult)
            : base(startResult.ToString())
        {
            StartResult = startResult;
        }
    }
}

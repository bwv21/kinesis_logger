using System;
using System.Collections.Generic;
using System.Linq;

namespace KLogger.Cores.Loggers
{
    /// <summary>
    ///     Logger의 설정 변경.
    ///     상대적으로 덜 중요한 동작이라고 판단하여 락을 사용하지 않고 처리한다.
    /// </summary>
    internal partial class Logger
    {
        internal void EnableIgnoreLog(Boolean enable)
        {
            UseIgnoreLogType = enable;
        }

        internal void AddIgnoreLogType(String logType)
        {
            if (String.IsNullOrEmpty(logType))
            {
                return;
            }

            logType = logType.ToLower();
            var newIgnoreLogTypes = new HashSet<String>(IgnoreLogTypes) { logType };
            IgnoreLogTypes = newIgnoreLogTypes;
        }

        internal void RemoveIgnoreLogType(String logType)
        {
            if (String.IsNullOrEmpty(logType))
            {
                return;
            }

            logType = logType.ToLower();
            var newIgnoreLogTypes = new HashSet<String>(IgnoreLogTypes);
            newIgnoreLogTypes.Remove(logType);
            IgnoreLogTypes = newIgnoreLogTypes;
        }

        internal IEnumerable<String> GetIgnoreLogTypes()
        {
            return IgnoreLogTypes?.ToArray();
        }
    }
}

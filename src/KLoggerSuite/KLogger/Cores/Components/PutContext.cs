using System;

namespace KLogger.Cores.Components
{
    internal class PutContext
    {
        internal PutLog PutLog { get; }
        internal Int32 RetryCount { get; }

        internal PutContext(PutLog putLog, Int32 retryCount)
        {
            PutLog = putLog;
            RetryCount = retryCount;
        }
    }
}

using System;
using KLogger.Cores.Components;

namespace KLogger.Cores.Structures
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

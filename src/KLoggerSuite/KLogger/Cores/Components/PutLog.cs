using System;
using KLogger.Types;

namespace KLogger.Cores.Components
{
    internal class PutLog
    {
        internal ILog[] RawLogs { get; }
        internal Byte[][] EncodedLogs { get; }
        internal Int32 TotalEncodedLogByte { get; }

        internal PutLog(ILog[] rawLogs, Byte[][] encodedLogs, Int32 totalEncodedLogByte)
        {
            RawLogs = rawLogs;
            EncodedLogs = encodedLogs;
            TotalEncodedLogByte = totalEncodedLogByte;
        }
    }
}

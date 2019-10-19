using System;

namespace KLogger.Libs
{
    // Kinesis 와의 호환을 위해 UTC 만을 사용한다.
    internal static class Now
    {
        private static readonly DateTime BaseDateTime = new DateTime(1970, 1, 1);

        public static DateTime NowDateTime()
        {
            return System.DateTime.UtcNow;
        }

        public static Int32 TimestampSec()
        {
            return (Int32)DateTime.UtcNow.Subtract(BaseDateTime).TotalSeconds;
        }

        public static Int64 TimestampMS()
        {
            return (Int64)DateTime.UtcNow.Subtract(BaseDateTime).TotalMilliseconds;
        }

        public static Int64 TimestampNS()
        {
            return (Int64)(DateTime.UtcNow.Subtract(BaseDateTime).TotalMilliseconds * 1000000);
        }

        public static Int32 TimestampSec(DateTime utcNow)
        {
            return (Int32)utcNow.Subtract(BaseDateTime).TotalSeconds;
        }

        public static Int64 TimestampMS(DateTime utcNow)
        {
            return (Int64)utcNow.Subtract(BaseDateTime).TotalMilliseconds;
        }

        public static Int64 TimestampNS(DateTime utcNow)
        {
            return (Int64)(utcNow.Subtract(BaseDateTime).TotalMilliseconds * 1000000);
        }
    }
}

using System;
using KLogger.Types;
using Newtonsoft.Json;

namespace KLogger.Cores.Components
{
    internal class Log : ILog
    {
        public Int64 Sequence { get; set; }
        public Int32 TimeStamp { get; set; }
        public Int64 TimeStampNS { get; set; }
        public String LogType { get; set; }
        public Object LogObject { get; set; }
        [JsonIgnore]
        public Boolean IsRawString { get; set; }
    }
}
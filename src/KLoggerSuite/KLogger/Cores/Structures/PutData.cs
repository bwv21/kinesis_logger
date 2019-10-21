using System;
using KLogger.Libs;
using Newtonsoft.Json;

namespace KLogger.Cores.Structures
{
    internal class PutData
    {
        public String ID { get; set; }
        public String InstanceID { get; set; }
        public Int64 Sequence { get; set; }
        public Int32 TimeStamp { get; set; }
        public Int64 TimeStampNS { get; set; }
        public String LogType { get; set; }

        // CompressedLog과 Log은 둘 중에 하나만 존재한다.
        public String CompressedLog { get; set; }           // 압축을 하면 전처리람다가 JSON을 파싱해야 하므로 "로 감싸야 한다.
        [JsonConverter(typeof(PlainJsonStringConverter))]   
        public String Log { get; set; }                     // 압축을 하지 않으면 "로 감싸지 않는다.
    }
}

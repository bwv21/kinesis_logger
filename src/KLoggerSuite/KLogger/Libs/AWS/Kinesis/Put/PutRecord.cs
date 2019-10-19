using System;

// http: //docs.aws.amazon.com/ko_kr/kinesis/latest/APIReference/API_PutRecords.html
namespace KLogger.Libs.AWS.Kinesis.Put
{
    internal class PutRecord
    {
        public Byte[] Data { get; set; }
        public String PartitionKey { get; set; }
    }
}

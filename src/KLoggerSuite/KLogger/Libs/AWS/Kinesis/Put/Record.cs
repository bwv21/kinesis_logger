using System;

// http: //docs.aws.amazon.com/ko_kr/kinesis/latest/APIReference/API_PutRecords.html
namespace KLogger.Libs.AWS.Kinesis.Put
{
    internal class Record
    {
        public String SequenceNumber { get; set; }
        public String ShardId { get; set; }
        public String ErrorCode { get; set; }
        public String ErrorMessage { get; set; }
    }
}

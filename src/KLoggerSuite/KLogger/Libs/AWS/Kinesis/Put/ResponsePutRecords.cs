using System;
using System.Collections.Generic;

// http: //docs.aws.amazon.com/ko_kr/kinesis/latest/APIReference/API_PutRecords.html
namespace KLogger.Libs.AWS.Kinesis.Put
{
    internal class ResponsePutRecords
    {
        public Int32 FailedRecordCount { get; set; }
        public List<Record> Records { get; set; }
    }
}

/*
{
	"FailedRecordCount": 0,
	"Records": [{
			"SequenceNumber": "49574739672520991407984453969876117890616362772224540882",
			"ShardId": "shardId-000000000013"
		}, {
			"SequenceNumber": "49574739672520991407984453969877326816435977401399247058",
			"ShardId": "shardId-000000000013"
		}, {
			"SequenceNumber": "49574739672520991407984453969878535742255592030573953234",
			"ShardId": "shardId-000000000013"
		}, {
			"SequenceNumber": "49574739683604461771654173671138371842208418285991493858",
			"ShardId": "shardId-000000000014"
		}
	]
}
*/

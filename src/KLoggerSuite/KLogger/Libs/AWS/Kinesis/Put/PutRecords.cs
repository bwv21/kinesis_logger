using System;

// http: //docs.aws.amazon.com/ko_kr/kinesis/latest/APIReference/API_PutRecords.html
namespace KLogger.Libs.AWS.Kinesis.Put
{
	internal class PutRecords
	{
		public PutRecord[] Records { get; set; }
		public String StreamName { get; set; }
	}
}

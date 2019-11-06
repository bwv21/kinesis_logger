using System;
using System.Text;
using Newtonsoft.Json;

namespace KLogger.Libs.AWS.Kinesis.Describe
{
    internal class DescribeStreamAPI : BaseKinesisAPI
    {
        public DescribeStreamAPI(String region, 
                                 String accessID,
                                 String secretKey, 
                                 String streamName, 
                                 RequestCompletedDelegate requestCompleted)
            : base(region, accessID, secretKey, streamName, requestCompleted)
        {
        }

        public void DescribeStream()
        {
            var describeStreamSummary = new
                                        {
                                            StreamName = StreamName
                                        };

            String postString = JsonConvert.SerializeObject(describeStreamSummary);
            Byte[] post = Encoding.UTF8.GetBytes(postString);

            Request("DescribeStreamSummary", post, null);
        }
    }
}

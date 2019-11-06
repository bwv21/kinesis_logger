using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace KLogger.Libs.AWS.Kinesis.Put
{
    internal class PutAPI : BaseKinesisAPI
    {
        public PutAPI(String region,
                      String accessID,
                      String secretKey,
                      String streamName,
                      RequestCompletedDelegate requestCompleted)
            : base(region, accessID, secretKey, streamName, requestCompleted)
        {
        }

        public Byte[] CreatePost(Byte[][] payloads)
        {
            Int32 recordCount = payloads.Length;
            if (recordCount <= 0)
            {
                return null;
            }

            var putRecords = new PutRecords
                             {
                                 Records = new PutRecord[recordCount],
                                 StreamName = StreamName
                             };

            using (SHA1 sha = SHA1.Create())
            {
                for (Int32 i = 0; i < recordCount; ++i)
                {
                    Byte[] data = payloads[i];
                    if (data == null)
                    {
                        continue;
                    }

                    putRecords.Records[i] = new PutRecord
                                            {
                                                Data = data,
                                                PartitionKey = Convert.ToBase64String(sha.ComputeHash(data))
                                            };
                }
            }

            String postString = JsonConvert.SerializeObject(putRecords);
            Byte[] post = Encoding.UTF8.GetBytes(postString);

            // data의 합과 비교해 보면, 약 1.4배 정도 post가 더 크다.
            return post;
        }

        // http://docs.aws.amazon.com/kinesis/latest/APIReference/API_PutRecords.html
        public void Put(Byte[] post, Object context)
        {
            Request("PutRecords", post, context);
        }
    }
}

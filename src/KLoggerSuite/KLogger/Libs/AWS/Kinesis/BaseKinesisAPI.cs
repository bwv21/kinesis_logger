using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace KLogger.Libs.AWS.Kinesis
{
    internal abstract class BaseKinesisAPI
    {
        public delegate void RequestCompletedDelegate(UploadDataCompletedEventArgs args, Object context);

        protected readonly String Region;
        protected readonly String AccessID;
        protected readonly String SecretKey;
        protected readonly String StreamName;
        protected readonly Uri EndPointUri;
        protected readonly RequestCompletedDelegate RequestCompleted;

        private readonly PostUtil _postUtil = new PostUtil();

        protected BaseKinesisAPI(String region, String accessID, String secretKey, String streamName, RequestCompletedDelegate requestCompleted)
        {
            Region = region;
            AccessID = accessID;
            SecretKey = secretKey;
            StreamName = streamName;
            EndPointUri = new Uri($"https://kinesis.{Region}.amazonaws.com");
            RequestCompleted = requestCompleted;
        }

        protected void Request(String api, Byte[] post, Object context)
        {
            using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
            {
                Dictionary<String, String> headers = _postUtil.CreateHeader(post,
                                                                            EndPointUri,
                                                                            "kinesis",
                                                                            $"Kinesis_20131202.{api}",
                                                                            Region,
                                                                            AccessID,
                                                                            SecretKey);

                foreach (var header in headers)
                {
                    webClient.Headers.Add(header.Key, header.Value);
                }

                webClient.UploadDataCompleted += (_, args) => RequestCompleted?.Invoke(args, context);

                webClient.UploadDataAsync(EndPointUri, "POST", post);
            }
        }
    }
}

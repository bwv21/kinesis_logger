using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace KLogger.Libs.AWS.Kinesis
{
    internal abstract class BaseKinesisAPI
    {
        public delegate void RequestCompletedDelegate(UploadDataCompletedEventArgs args, Object context);

        protected readonly PostUtil _postUtil = new PostUtil();

        protected readonly String _region;
        protected readonly String _accessID;
        protected readonly String _secretKey;
        protected readonly String _streamName;
        protected readonly Uri _endPointUri;
        protected readonly RequestCompletedDelegate _requestCompleted;

        protected BaseKinesisAPI(String region, String accessID, String secretKey, String streamName, RequestCompletedDelegate requestCompleted)
        {
            _region = region;
            _accessID = accessID;
            _secretKey = secretKey;
            _streamName = streamName;
            _endPointUri = new Uri($"https://kinesis.{_region}.amazonaws.com");
            _requestCompleted = requestCompleted;
        }

        protected void Request(String api, Byte[] post, Object context)
        {
            using (var webClient = new WebClient { Encoding = Encoding.UTF8 })
            {
                Dictionary<String, String> headers = _postUtil.CreateHeader(post,
                                                                            _endPointUri,
                                                                            "kinesis",
                                                                            $"Kinesis_20131202.{api}",
                                                                            _region,
                                                                            _accessID,
                                                                            _secretKey);

                foreach (var header in headers)
                {
                    webClient.Headers.Add(header.Key, header.Value);
                }

                webClient.UploadDataCompleted += (_, args) => _requestCompleted?.Invoke(args, context);

                webClient.UploadDataAsync(_endPointUri, "POST", post);
            }
        }
    }
}

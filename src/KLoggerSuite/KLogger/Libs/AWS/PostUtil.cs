using System;
using System.Collections.Generic;
using KLogger.Libs.AWS.Auth;

namespace KLogger.Libs.AWS
{
    internal class PostUtil
    {
        private readonly Object _lock = new Object();

        // AWS API를 사용할 수 있는 인증 정보를 포함한 헤더를 생성한다.
        public Dictionary<String, String> CreateHeader(Byte[] postData,
                                                       Uri uri,
                                                       String service,
                                                       String xAmzTarget,
                                                       String region,
                                                       String accessID,
                                                       String secretKey)
        {
            var headers = new Dictionary<String, String>
                          {
                              { "x-amz-target", xAmzTarget },
                              { "content-type", "application/x-amz-json-1.1" }
                          };

            String authorization = CreateAuthorization(postData,
                                                       headers,
                                                       uri,
                                                       service,
                                                       region,
                                                       accessID,
                                                       secretKey);

            headers.Add("Authorization", authorization);

            return headers;
        }

        private String CreateAuthorization(Byte[] postData,
                                           IDictionary<String, String> headers,
                                           Uri uri,
                                           String service,
                                           String region,
                                           String accessID,
                                           String secretKey)
        {
            // lock이 없으면 400에러(Protocol Error)가 발생할 수 있다.
            lock (_lock)
            {
                Byte[] contentHash = AWS4SignerBase.CanonicalRequestHashAlgorithm.ComputeHash(postData);
                String contentHashString = AWS4SignerBase.ToHexString(contentHash, true);

                var signer = new AWS4SignerForPOST
                             {
                                 EndpointUri = uri,
                                 HttpMethod = "POST",
                                 Service = service,
                                 Region = region
                             };

                String authorization = signer.ComputeSignature(headers, String.Empty, contentHashString, accessID, secretKey);
                return authorization;
            }
        }
    }
}

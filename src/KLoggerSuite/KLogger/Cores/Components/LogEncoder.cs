using System;
using System.Text;
using KLogger.Configs;
using KLogger.Cores.Loggers;
using KLogger.Cores.Structures;
using KLogger.Libs;
using KLogger.Types;

namespace KLogger.Cores.Components
{
    internal class LogEncoder
    {
        private readonly JsonSerializer _jsonSerializer = new JsonSerializer();

        private String _id;
        private String _instanceID;
        private Int32 _compressLogThresholdByte;
        private ErrorCounter _errorCounter;

        internal void Initialize(Logger logger)
        {
            _id = logger.Config.ID;
            _instanceID = logger.InstanceID;
            _compressLogThresholdByte = logger.Config.CompressLogThresholdByte;
            _errorCounter = logger.ErrorCounter;
        }

        internal Byte[] Encode(Log log)
        {
            try
            {
                return EncodeImpl(log);
            }
            catch (Exception exception)
            {
                _errorCounter.RaiseError($"Encode Error: {exception.Message}");
                return null;
            }
        }

        internal Byte[] EncodeImpl(Log log)
        {
            String logObjectStr = null;

            if (log.LogObject is String str)
            {
                if (log.IsRawString)
                {
                    // String을 그대로 저장하면 Athena에서 다른 JSON로그와 문제가 되므로 변환한다.
                    log.LogObject = new StringLogObject { raw = str };  // { "raw" : "로그문자열" }
                }
                else
                {
                    // String이지만 JSON으로 변환된 상태이므로 그대로 전송한다.
                    logObjectStr = str;
                }
            }

            logObjectStr = logObjectStr ?? _jsonSerializer.Serialize(log.LogObject);

            String compressedLog = null;
            String plainLog = null;

            if (_compressLogThresholdByte < logObjectStr.Length)
            {
                compressedLog = StringCompressor.Compress(logObjectStr);
            }
            else
            {
                plainLog = logObjectStr;
            }

            var putString = new StringBuilder(_jsonSerializer.Serialize(new PutData
                                                                        {
                                                                            ID = _id,
                                                                            InstanceID = _instanceID,
                                                                            Sequence = log.Sequence,
                                                                            TimeStamp = log.TimeStamp,
                                                                            TimeStampNS = log.TimeStampNS,
                                                                            LogType = log.LogType,
                                                                            CompressedLog = compressedLog,
                                                                            Log = plainLog
                                                                        }));

            putString.Append(Const.LOG_DELIMITER);

            Byte[] encodedLog = Encoding.UTF8.GetBytes(putString.ToString());

            return encodedLog;
        }
    }
}

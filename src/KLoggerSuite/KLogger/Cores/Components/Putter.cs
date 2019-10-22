using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using KLogger.Configs;
using KLogger.Cores.Loggers;
using KLogger.Cores.Structures;
using KLogger.Libs;
using KLogger.Libs.AWS.Kinesis.Put;
using KLogger.Libs.Reporters;
using KLogger.Types;
using Newtonsoft.Json;

namespace KLogger.Cores.Components
{
    // https://docs.aws.amazon.com/ko_kr/streams/latest/dev/developing-producers-with-sdk.html#kinesis-using-sdk-java-putrecords    // 자바지만 참고할만 하다.
    // https://docs.aws.amazon.com/ko_kr/kinesis/latest/APIReference/API_PutRecords.html    // 기본적인 매뉴얼.
    internal class Putter
    {
        private const Int32 RETRY_SLEEP_MS = 1000;
        private const Int32 MAX_RETRY_RETRY_SLEEP_MS = 30000;

        private Config _config;
        private Reporter _reporter;
        private Watcher _watcher;
        private ErrorCounter _errorCounter;
        private CompletePutNotifier _completePutNotifier;
        private ThroughputController _throughputController;
        private PutAPI _kinesisPutAPI;
        
        internal void Initialize(Logger logger)
        {
            _config = logger.Config;
            _reporter = logger.Reporter;
            _watcher = logger.Watcher;
            _errorCounter = logger.ErrorCounter;
            _completePutNotifier = logger.CompletePutNotifier;
            _throughputController = logger.ThroughputController;
            _kinesisPutAPI = new PutAPI(_config.AWSs.Region, _config.AWSs.DecryptedAccessID, _config.AWSs.DecryptedSecretKey, _config.AWSs.KinesisStreamName, OnPutRequestCompleted);
        }

        internal void Put(PutLog putLog, Int32 retryCount = 0)
        {
            if (CheckPutLog(putLog) == false)
            {
                return;
            }

            PutInternal(new PutContext(putLog, retryCount));
        }

        private Boolean CheckPutLog(PutLog putLog)
        {
            if (putLog == null)
            {
                _reporter.Warn($"{nameof(putLog)} is null!");
                return false;
            }

            if (putLog.RawLogs == null)
            {
                _reporter.Warn($"{nameof(putLog.RawLogs)} is null!");
                return false;
            }

            if (putLog.EncodedLogs == null)
            {
                _reporter.Warn($"{nameof(putLog.EncodedLogs)} is null!");
                return false;
            }

            if (putLog.TotalEncodedLogByte <= 0)
            {
                _reporter.Warn($"{nameof(putLog.TotalEncodedLogByte)}({putLog.TotalEncodedLogByte}) <= 0");
                return false;
            }

            if (putLog.RawLogs.Length != putLog.EncodedLogs.Length)
            {
                _reporter.Warn($"{nameof(putLog.RawLogs.Length)}({putLog.RawLogs.Length}) != {nameof(putLog.EncodedLogs.Length)}({putLog.EncodedLogs.Length})");
                return false;
            }

            return true;
        }

        private void PutInternal(PutContext putContext)
        {
            try
            {
                Byte[] post = _kinesisPutAPI.CreatePost(putContext.PutLog.EncodedLogs);

                _kinesisPutAPI.Put(post, putContext);

                if (_config.UseThroughputControl == 1)
                {
                    _throughputController.UseCapacity(putContext.PutLog.TotalEncodedLogByte, putContext.PutLog.EncodedLogs.Length);
                }
            }
            catch (Exception exception)
            {
                _errorCounter.RaiseError($"{exception.Message}\n{exception.InnerException?.Message}");
                throw;
            }
        }

        private void OnPutRequestCompleted(UploadDataCompletedEventArgs args, Object context)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            String error = null;
            String result = null;
            if (args.Error == null)
            {
                result = Encoding.UTF8.GetString(args.Result);
            }
            else
            {
                error = args.Error.Message;
            }

            OnPutRequestCompletedImpl(error, result, context as PutContext);
        }

        // HTTP통신의 완료 콜백(비동기, 다른 스레드 가능성). HTTP통신이 성공해도 Kinesis전송도 성공한 것은 아니다.
        private void OnPutRequestCompletedImpl(String error, String uploadResult, PutContext putContext)
        {
            Boolean uploadSuccess = false;

            try
            {
                if (String.IsNullOrEmpty(error))
                {
                    uploadSuccess = true;
                    PostUploadDataCompleted(uploadResult, putContext);
                }
                else
                {
                    _errorCounter.RaiseError(error);

                    DebugLog.Log(error, "klogger:putter");
                }
            }
            catch (Exception exception)
            {
                _errorCounter.RaiseError($"{exception.Message}\n{exception.InnerException?.Message}");

                DebugLog.Log($"{exception.Message}/{exception.InnerException?.Message}", "klogger:putter");
            }
            finally
            {
                if (uploadSuccess == false)
                {
                    RetryPutLog(putContext.PutLog, putContext.RetryCount);
                }
            }
        }

        private void PostUploadDataCompleted(String result, PutContext putContext)
        {
            if (0 < putContext.RetryCount)
            {
                SendSlackRetrySuccess(putContext.RetryCount, putContext.PutLog.RawLogs.Length);
            }

            var response = JsonConvert.DeserializeObject<ResponsePutRecords>(result);
            if (response == null)
            {
                _errorCounter.RaiseError($"invalid put kinesis response!\nresult: {result}");
                return;
            }

            // <성공, 실패> 로 분할한다.
            var successAndFail = SplitSuccessAndFailPutLog(response, putContext);
            PutLog successPutLog = successAndFail.Item1;
            PutLog failPutLog = successAndFail.Item2;

            if (successPutLog != null)
            {
                _errorCounter.ResetSerialError();
                _completePutNotifier.Push(successAndFail.Item1.RawLogs, CompletePutNoticeResultType.Success);
            }

            if (failPutLog != null)
            {
                RetryPutLog(successAndFail.Item2, putContext.RetryCount);

                if (_config.UseThroughputControl == 1)
                {
                    _throughputController.EnableThrottling(true);
                }

                _reporter.Info($"Partial Fail PutKinesis Fail/Total(`{response.FailedRecordCount.ToString()}/{response.Records.Count.ToString()}`)");

                DebugLog.Log($"Partial Fail PutKinesis (`{response.FailedRecordCount.ToString()}/{response.Records.Count.ToString()}`)", "klogger:putter");
            }
            else
            {
                if (_config.UseThroughputControl == 1)
                {
                    _throughputController.EnableThrottling(false);
                }
            }

            UpdateWatcher(response);

            DebugLog.Log($"KinesisResponse: RecordCount:{response.Records.Count.ToString()}, FailCount({response.FailedRecordCount.ToString()})", "klogger:putter");
        }

        // <성공, 실패> 로 분할.
        private Tuple<PutLog, PutLog> SplitSuccessAndFailPutLog(ResponsePutRecords response, PutContext putContext)
        {
            // 모두 성공한 경우.
            if (response.FailedRecordCount <= 0)
            {
                return new Tuple<PutLog, PutLog>(putContext.PutLog, null);
            }

            // 모두 실패한 경우.
            if (response.Records.Count == response.FailedRecordCount)
            {
                return new Tuple<PutLog, PutLog>(null, putContext.PutLog);
            }

            Int32 totalCount = response.Records.Count;
            Int32 successCount = totalCount - response.FailedRecordCount;
            Int32 failCount = response.FailedRecordCount;
            
            var successRawLogs = new List<ILog>(successCount);
            var successEncodedLogs = new List<Byte[]>(successCount);
            Int32 successTotalEncodedLogByte = 0;

            var failRawLogs = new List<ILog>(failCount);
            var failEncodedLogs = new List<Byte[]>(failCount);
            Int32 failTotalEncodedLogByte = 0;

            for (Int32 i = 0; i < totalCount; ++i)
            {
                ILog rawLogs = putContext.PutLog.RawLogs[i];
                Byte[] encodedLog = putContext.PutLog.EncodedLogs[i];

                // 중요: Kinesis의 응답이 보낸 순서와 일치한다(Kinesis가 보낸 순서와 응답을 맞춰서 준다).
                Record record = response.Records[i];
                if (String.IsNullOrEmpty(record.ErrorCode))
                {
                    successRawLogs.Add(rawLogs);
                    successEncodedLogs.Add(encodedLog);
                    successTotalEncodedLogByte += encodedLog.Length;
                }
                else
                {
                    failRawLogs.Add(rawLogs);
                    failEncodedLogs.Add(encodedLog);
                    failTotalEncodedLogByte += encodedLog.Length;
                }
            }

            var successPutLog = new PutLog(successRawLogs.ToArray(), successEncodedLogs.ToArray(), successTotalEncodedLogByte);
            var failPutLog = new PutLog(failRawLogs.ToArray(), failEncodedLogs.ToArray(), failTotalEncodedLogByte);
            return new Tuple<PutLog, PutLog>(successPutLog, failPutLog);
        }

        private void SendSlackRetrySuccess(Int32 retryCount, Int32 logCount)
        {
            String text = $"Success Retry Put Kinesis! RetryCount: `{retryCount.ToString()}`, LogCount: `{logCount.ToString()}`";

            if (retryCount <= 2)
            {
                _reporter.Debug(text);
            }
            else if (retryCount <= 5)
            {
                _reporter.Info(text);
            }
            else
            {
                _reporter.Warn(text);
            }
        }

        private void RetryPutLog(PutLog putLog, Int32 retryCount)
        {
            if (_config.MaxRetrySendCount <= retryCount)
            {
                DropLog(putLog, retryCount);
                return;
            }

            // 대부분 ProvisionedThroughputExceededException 오류이므로 기다렸다 재시도한다.
            ThreadPool.QueueUserWorkItem(_ => SleepAndPut(putLog, retryCount + 1));
        }

        private void DropLog(PutLog putLog, Int32 retryCount)
        {
            Boolean isSuccess = _completePutNotifier.Push(putLog.RawLogs, CompletePutNoticeResultType.FailRetry);
            if (isSuccess == false)
            {
                _reporter.Error($"Fail CompletePutNotifier.Push ({putLog.RawLogs.Length})");
            }

            _watcher.DropLog(putLog.RawLogs.Length);

            _reporter.Error($"Loss Log: {putLog.RawLogs.Length}, RetryCount: {retryCount}");

            DebugLog.Log($"Drop Log({putLog.EncodedLogs.Length}, {putLog.TotalEncodedLogByte}).", "klogger:putter");
        }

        private void SleepAndPut(PutLog putLog, Int32 retryCount)
        {
            Thread.Sleep(Math.Min(MAX_RETRY_RETRY_SLEEP_MS, RETRY_SLEEP_MS * retryCount));
            
            Put(putLog, retryCount);

            DebugLog.Log($"SleepAndPut. RetryCount: {retryCount.ToString()}", "klogger:putter");
        }

        private void UpdateWatcher(ResponsePutRecords response)
        {
            Int32 successCount = response.Records.Count - response.FailedRecordCount;
            _watcher.CompletePut(successCount);
            _watcher.UpdateKinesisShard(response);
        }
    }
}

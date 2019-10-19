using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using KLogger.Configs;
using KLogger.Cores.Exceptions;
using KLogger.Cores.Loggers;
using KLogger.Types;

namespace KLogger
{
    /// <summary>
    /// 로거를 생성하고 제어한다.
    /// </summary>
    public class KLoggerAPI
    {
        private readonly Logger _logger;

        /// <summary> 로거의 빌드 타입. </summary>
        public BuildType BuildType => _logger.BuildType;

        /// <summary> 로거의 현재 상태. </summary>
        public StateType State => _logger.State;

        /// <summary> 무시할 로그 타입 목록(set)으로 소문자로 변환된다. </summary>
        public IEnumerable<String> IgnoreLogTypes => _logger.GetIgnoreLogTypes();

        /// <summary> 연속해서 발생한 에러 개수. </summary>
        public Int32 SerialErrorCount => _logger.ErrorCounter.SerialErrorCount;

        /// <summary> 로거에서 발생한 모든 에러 개수. </summary>
        public Int32 TotalErrorCount => _logger.ErrorCounter.TotalErrorCount;

        /// <summary> 로거 내부의 대기 큐 안에 있는 로그 수. </summary>
        public Int32 LogCountInQueue => _logger.LogCountInQueue;

        /// <summary> 전송에 성공한 로그 수. </summary>
        public Int64 SuccessLogCount => _logger.Watcher.SuccessLogCount;

        /// <summary> 전송 진행 중(재시도 포함)인 로그 수. </summary>
        public Int64 PendingLogCount => _logger.Watcher.PendingLogCount;

        /// <summary> 전송에 실패하여 버린 로그 수(푸시 실패는 세지 않는다). </summary>
        public Int64 FailLogCount => _logger.Watcher.FailLogCount;

        /// <summary> 요청한 타입별 로그 수(완료와 무관). </summary>
        public ConcurrentDictionary<String, Int32> LogTypeToCount => _logger.Watcher.LogTypeToCount;

        /// <summary>
        /// 설정 파일로부터 로거를 생성한다.
        /// </summary>
        /// <param name="configPath">
        /// 설정 파일의 경로.
        /// </param>
        /// <param name="onCompletePut">
        /// 처리 완료한 로그에 대한 콜백(<see cref="CompletePutDelegate"/>).
        /// </param>
        /// <param name="completePutNotifyType">
        /// 통지받을 처리 완료 결과 종류.
        /// </param>
        public KLoggerAPI(String configPath, CompletePutDelegate onCompletePut, CompletePutNotifyType completePutNotifyType = CompletePutNotifyType.FailOnly)
        {
            _logger = new Logger(configPath, onCompletePut, completePutNotifyType);
        }

        /// <summary>
        /// <see cref="Config"/> 부터 로거를 생성한다.
        /// </summary>
        /// <param name="config">
        /// <see cref="Config.Create"/> 로 생성한 인스턴스.
        /// </param>
        /// <param name="onCompletePut">
        /// 처리 완료한 로그에 대한 콜백(<see cref="CompletePutDelegate"/>).
        /// </param>
        /// <param name="completePutNotifyType">
        /// <see cref="onCompletePut"/> 으로 통지받을 결과 종류.
        /// </param>
        public KLoggerAPI(Config config, CompletePutDelegate onCompletePut, CompletePutNotifyType completePutNotifyType = CompletePutNotifyType.FailOnly)
        {
            _logger = new Logger(config, onCompletePut, completePutNotifyType);
        }

        /// <summary>
        /// 로거를 시작한다. <see cref="StateType.Stop"/> 상태에서만 유효하다.
        /// </summary>
        /// <returns>
        /// 시작 결과(<see cref="StartResultType"/>).
        /// </returns>
        public StartResultType Start()
        {
            return _logger.Start();
        }

        /// <summary>
        /// 로거를 시작한다. 실패하면 예외(<see cref="LoggerException"/>)를 발생시킨다.
        /// </summary>
        public void StartIfFailThrow()
        {
            StartResultType startResultType = _logger.Start();
            if (startResultType != StartResultType.Success)
            {
                throw new LoggerException($"{nameof(StartIfFailThrow)}: {startResultType}");
            }
        }

        /// <summary>
        /// 로거를 정지한다. <see cref="StateType.Start"/>, <see cref="StateType.Pause"/> 상태에서만 유효하다.
        /// 모든 스레드를 정지하고 큐에 남아있는 로그를 즉시 전송한다.
        /// </summary>
        public void Stop()
        {
            _logger.Stop();
        }

        /// <summary>
        /// 로거를 일시정지한다. <see cref="StateType.Start"/> 상태에서만 유효하다.
        /// 로거의 모든 스레드는 살아있지만 더 이상 푸시를 할 수 없고, 큐에 있는 로그도 처리하지 않는다.
        /// 이미 전송을 시작한 로그와 재시도하고 있는 로그에는 영향을 주지 않는다.
        /// <see cref="Resume"/> 으로 재개할 수 있다.
        /// </summary>
        public void Pause()
        {
            _logger.Pause();
        }

        /// <summary>
        /// 로거를 재개한다. <see cref="StateType.Pause"/> 상태에서만 유효하다.
        /// 다시 푸시를 할 수 있고, 큐에 있는 로그를 처리하기 시작한다.
        /// </summary>
        public void Resume()
        {
            _logger.Resume();
        }

        /// <summary>
        /// 로그를 푸시한다
        /// </summary>
        /// <param name="type">
        /// 로그 타입으로 null 일 수 없다.
        /// </param>
        /// <param name="log">
        /// 로그 인스턴스로 null 일 수 없다. String 타입인 경우 <see cref="StringLogObject"/> 로 래핑된다.
        /// </param>
        /// <returns>
        /// 성공했으면 0 보다 큰 시퀀스 반환. 실패(무시포함)하면 -1 반환.
        /// </returns>
        public Int64 Push(String type, Object log)
        {
            return _logger.Push(type, log);
        }

        /// <summary>
        /// JSON문자열인 로그를 푸시한다. JSON이 아니면 푸시에 실패한다.
        /// </summary>
        /// <param name="type">
        /// 로그 타입으로 null 일 수 없다.
        /// </param>
        /// <param name="jsonString">
        /// JSON 형식의 문자열(String)로 null 일 수 없다.
        /// </param>
        /// <returns>
        /// 성공했으면 0 보다 큰 시퀀스 반환. 실패(무시포함)하면 -1 반환.
        /// </returns>
        public Int64 PushJsonString(String type, String jsonString)
        {
            return _logger.PushJsonString(type, jsonString);
        }

        /// <summary>
        /// 로거의 설정(<see cref="Config"/>)을 <see cref="ReportLevelType.Info"/> 로 출력한다.
        /// </summary>
        public void ReportConfig()
        {
            _logger.Reporter.Info($"Config\n```{_logger.Config.ConfigStringPretty}```");
        }

        /// <summary>
        /// 로거의 종합 상태를 <see cref="ReportLevelType.Info"/> 로 출력한다.
        /// 즉시 동작하지 않고 다음 모니터링 틱(<see cref="Config.Watcher.IntervalMS"/>)이후에 출력하도록 예약한다.
        /// </summary>
        public void ReportStatus()
        {
            _logger.Watcher.EnableForceReport();
        }

        /// <summary>
        /// 로그 타입별 개수를 <see cref="ReportLevelType.Info"/> 로 출력한다.
        /// </summary>
        public void ReportLogTypeToCount()
        {
            _logger.Watcher.ReportLogTypeToCount();
        }

        /// <summary>
        /// Kinesis Shard 당 사용량을 <see cref="ReportLevelType.Info"/> 로 출력한다.
        /// </summary>
        public void ReportKinesisShardUsage()
        {
            _logger.Watcher.ReportKinesisShardUsage();
        }

        /// <summary>
        /// 로거의 모든(연속, 총합) 에러를 0으로 초기화한다.
        /// </summary>
        public void ResetError()
        {
            _logger.ErrorCounter.ResetAllError();
        }

        /// <summary>
        /// 로그 타입별 개수를 0으로 초기화한다.
        /// </summary>
        public void ResetLogTypeToCount()
        {
            _logger.Watcher.ResetLogTypeToCount();
        }

        /// <summary>
        /// 로그 타입별 무시 기능을 켜거나 끈다. 이전 상태를 무시한다.
        /// </summary>
        /// <param name="enable">
        /// true면 켜고, false면 끈다.
        /// </param>
        public void EnableIgnoreLog(Boolean enable)
        {
            _logger.EnableIgnoreLog(enable);
        }

        /// <summary>
        /// 무시할 로그 타입을 추가한다.
        /// </summary>
        /// <param name="logType">
        /// 추가할 로그 타입으로 소문자로 변환된다.
        /// </param>
        public void AddIgnoreLogType(String logType)
        {
            _logger.AddIgnoreLogType(logType);
        }

        /// <summary>
        /// 무시할 로그 타입을 제거한다.
        /// </summary>
        /// <param name="logType">
        /// 제거할 로그 타입으로 소문자로 변환된다.
        /// </param>
        public void RemoveIgnoreLogType(String logType)
        {
            _logger.RemoveIgnoreLogType(logType);
        }
    }
}

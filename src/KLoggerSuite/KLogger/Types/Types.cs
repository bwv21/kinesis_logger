using System.Collections.Generic;

namespace KLogger.Types
{
    /// <summary>
    /// <para> 로그 전송의 최종 완료 콜백으로 여러 스레드에서 불릴 수 있으며 순서를 보장하지 않는다. </para>
    /// 설정(<see cref="CompletePutNoticeType"/>) 값에 따라 통지가 오지 않거나 특정 처리 결과의 통지만 온다.
    /// </summary>
    /// <param name="completePutNotices">
    /// 한 개 이상의 처리를 완료한 로그.
    /// </param>
    public delegate void NoticeCompletePutDelegate(IEnumerable<CompletePutNotice> completePutNotices);

    /// <summary> <see cref="NoticeCompletePutDelegate"/> 에서 통지받을 결과의 종류. </summary>
    public enum CompletePutNoticeType
    {
        /// <summary> 통지를 받지 않는다. </summary>
        None = -1,

        /// <summary> 실패(<see cref="CompletePutNoticeResultType.Success"/> 외 나머지) 통지만 받는다(권장). </summary>
        FailOnly,

        /// <summary> 성공(<see cref="CompletePutNoticeResultType.Success"/>) 통지만 받는다. </summary>
        SuccessOnly,

        /// <summary> 모든(성공, 실패) 통지를 받는다. </summary>
        Both
    }

    /// <summary> 로거 바이너리 빌드 타입. </summary>
    public enum BuildType
    {
        Debug,
        Release
    }

    /// <summary> 로거의 리포터 타입. </summary>
    public enum ReporterType
    {
        InvalidMin,

        /// <summary> 리포터 없음. 어디에도 출력하지 않는다. </summary>
        None,

        /// <summary> <see cref="System.Diagnostics.Debug"/> 를 사용하여 출력한다. </summary>
        Debug,

        /// <summary> <see cref="System.Console"/> 을 사용하여 출력한다 </summary>
        Console,

        /// <summary> 슬랙의 웹훅을 사용하여 출력한다. <see cref="Configs.Config.SlackConfig"/> 설정이 필요하다. </summary>
        Slack,

        InvalidMax
    }

    /// <summary> 로거의 리포터 레벨. 레벨 이상의 로그만 출력한다. </summary>
    public enum ReportLevelType
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    /// <summary> 로거의 상태. </summary>
    public enum StateType
    {
        /// <summary> 정지 및 초기 상태. </summary>
        Stop,

        /// <summary> 정지 프로세스를 진행 중인 상태. </summary>
        Stopping,

        /// <summary> 동작하고 있는 상태. </summary>
        Start,

        /// <summary> 시작 프로세스를 진행 중인 상태. </summary>
        Starting,

        /// <summary> 일시정지 상태. </summary>
        Pause
    }

    /// <summary> 로거의 시작(<see cref="KLoggerAPI.Start"/>) 호출 리턴 타입. </summary>
    public enum StartResultType
    {
        /// <summary> 정의되지 않음. </summary>
        Undefined = -1,

        /// <summary> 시작 성공. </summary>
        Success = 0,

        /// <summary> 시작 실패. 정지해 있지 않은 로거를 시작. </summary>
        NotStopped,

        /// <summary> 시작 실패. <see cref="Configs.Config"/> 에 오류가 있음. </summary>
        InvalidConfig,

        /// <summary> 시작 실패. <see cref="Configs.Config.SlackConfig"/> 에 오류가 있음. </summary>
        InvalidSlackWebhookUrl,

        /// <summary> 시작 실패. <see cref="Configs.Config.ReporterType"/> 이 올바르지 않음. </summary>
        InvalidReporterType,
    }

    /// <summary> 로그의 최종 처리 결과 타입. </summary>
    public enum CompletePutNoticeResultType
    {
        /// <summary> 전송 성공. </summary>
        Success = 0,

        /// <summary> 전송 실패. 로그 인코딩 실패. </summary>
        FailEncode,

        /// <summary> 전송 실패. 로그를 압축해도 <see cref="Configs.Config.MaxRecordByte"/> 보다 큼. </summary>
        TooLargeLogSize,

        /// <summary> 전송 실패. 재전송 횟수가 <see cref="Configs.Config.MaxRetrySendCount"/> 에 도달. </summary>
        FailRetry
    }
}

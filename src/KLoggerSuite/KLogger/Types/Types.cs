using System.Collections.Generic;

namespace KLogger.Types
{
    /// <summary>
    /// <para> 로그 전송의 최종 완료 콜백으로 여러 스레드에서 불릴 수 있으며 순서를 보장하지 않는다. </para>
    /// 로거의 설정(<see cref="CompletePutNotifyType"/>)에 값에 따라 통지가 오지 않거나 특정 타입의 통지만 온다.
    /// </summary>
    /// <param name="completePuts">
    /// 한 개 이상의 완료(성공 또는 실패)한 로그 정보를 담고있다.
    /// </param>
    public delegate void CompletePutDelegate(IEnumerable<CompletePut> completePuts);

    /// <summary> 통지받을 로그 처리 결과의 타입. <see cref="CompletePutDelegate"/> 에 언제 통지할지를 결정한다. </summary>
    public enum CompletePutNotifyType
    {
        /// <summary> 모든 통지를 받지 않는다. </summary>
        None = -1,

        /// <summary> 실패(<see cref="CompletePutType.Success"/> 외 나머지) 통지만 받는다(권장). </summary>
        FailOnly,

        /// <summary> 성공(<see cref="CompletePutType.Success"/>) 통지만 받는다. </summary>
        SuccessOnly,

        /// <summary> 성공 또는 실패의 모든 통지를 받는다. </summary>
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

    /// <summary> 로거의 현재 상태. </summary>
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

    /// <summary> 로거의 시작(<see cref="KLoggerAPI.Start"/>) 결과 리턴 타입. </summary>
    public enum StartResultType
    {
        Invalid = -1,

        /// <summary> 시작 성공. </summary>
        Success = 0,

        /// <summary> 정지해 있지 않은 로거를 시작. </summary>
        NotStopped,

        /// <summary> <see cref="Configs.Config"/> 에 오류가 있음. </summary>
        InvalidConfig,

        /// <summary> <see cref="Configs.Config.SlackConfig"/> 에 오류가 있음. </summary>
        InvalidSlackWebhookUrl,

        /// <summary> <see cref="Configs.Config.ReporterType"/> 이 올바르지 않음. </summary>
        InvalidReporterType,
    }

    /// <summary> 로그의 최종 처리 결과 타입. </summary>
    public enum CompletePutType
    {
        /// <summary> 전송 성공. </summary>
        Success = 0,

        /// <summary> 로그 인코딩에 실패. </summary>
        FailEncode,

        /// <summary> 인코딩 후 압축까지 했는데도 <see cref="Configs.Config.MaxRecordByte"/> 보다 커서 전송에 실패. </summary>
        TooLargeLogSize,

        /// <summary> 재전송 횟수가 <see cref="Configs.Config.MaxRetrySendCount"/> 에 도달하여 실패. </summary>
        FailRetry
    }
}

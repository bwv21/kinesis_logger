namespace KLogger.Types
{
    /// <summary>
    /// 로그 전송의 완료알림(<see cref="NoticeCompletePutDelegate"/>)의 인자로 하나의 결과에 대해 여러 로그가 있을 수 있다.
    /// </summary>
    public class CompletePutNotice
    {
        /// <summary> 같은 결과로 완료된 로그들. </summary>
        public ILog[] Logs { get; }

        /// <summary> 로그들의 처리 결과. </summary>
        public CompletePutNoticeResultType CompletePutNoticeResultType { get; }

        internal CompletePutNotice(ILog[] logs, CompletePutNoticeResultType completePutNoticeResultType)
        {
            Logs = logs;
            CompletePutNoticeResultType = completePutNoticeResultType;
        }
    }
}

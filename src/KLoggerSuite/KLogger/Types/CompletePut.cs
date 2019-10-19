namespace KLogger.Types
{
    /// <summary>
    /// 로그 전송의 완료알림(<see cref="CompletePutDelegate"/>)의 인자로 하나의 결과에 대한 여러 로그가 있을 수 있다.
    /// </summary>
    public class CompletePut
    {
        /// <summary> 완료된 로그들. </summary>
        public ILog[] Logs { get; }

        /// <summary> 완료된 로그들의 처리 결과. </summary>
        public CompletePutType CompletePutType { get; }

        public CompletePut(ILog[] logs, CompletePutType completePutType)
        {
            Logs = logs;
            CompletePutType = completePutType;
        }
    }
}

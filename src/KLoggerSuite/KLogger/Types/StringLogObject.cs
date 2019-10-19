using System;

namespace KLogger.Types
{
    /// <summary>
    /// <para> Athena에 객체로 만든 JSON이 아닌 String을 바로 넣으면 오류가 생기기 때문에 만든 래핑 클래스. </para>
    /// String을 바로 로그로 남기는 경우, 해당 클래스로 변환된다.
    /// </summary>
    public class StringLogObject
    {
        /// <summary> 원본 문자열 </summary>
        public String raw { get; set; } // 소문자는 의도한 것.
    }
}

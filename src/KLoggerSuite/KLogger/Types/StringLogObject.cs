using System;

namespace KLogger.Types
{
    /// <summary>
    /// <para> Athena에 JSON이 아닌, String을 바로 넣으면 조회시 오류가 생길 수 있기 때문에 만든 래핑 클래스. </para>
    /// String을 바로 로그로 남기는 경우, 로거가 해당 클래스로 변환한다.
    /// </summary>
    public class StringLogObject
    {
        /// <summary> 원본 문자열 </summary>
        public String raw { get; set; } // 소문자는 의도한 것.
    }
}

using System;

namespace KLogger.Types
{
    /// <summary>
    /// 로거가 다루는 로그 인터페이스.
    /// </summary>
    public interface ILog
    {
        /// <summary> Push할 때 리턴한 값. 하나의 로거 인스턴스 안에서만 유일함을 보장한다. </summary>
        Int64 Sequence { get; }

        /// <summary> Push할 때의 Timestamp(UTC). </summary>
        Int32 TimeStamp { get; }

        /// <summary> Push할 때 넘긴 타입 문자열. </summary>
        String LogType { get; }

        /// <summary> Push한 로그 인스턴스. </summary>
        Object LogObject { get; }
    }
}

using System;

namespace KLogger.Types
{
    /// <summary>
    /// 로거의 로그 인터페이스.
    /// </summary>
    public interface ILog
    {
        /// <summary> Push할 때 리턴한 값. </summary>
        Int64 Sequence { get; }

        /// <summary> Push할 때의 Timestamp(UTC). </summary>
        Int32 TimeStamp { get; }

        /// <summary> Push할 때 넘긴 타입 문자열. </summary>
        String LogType { get; }

        /// <summary> Push한 로그 인스턴스. </summary>
        Object LogObject { get; }
    }
}

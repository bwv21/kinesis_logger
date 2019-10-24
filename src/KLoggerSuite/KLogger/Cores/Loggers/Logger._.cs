using System;
using System.Data;
using KLogger.Configs;
using KLogger.Types;

namespace KLogger.Cores.Loggers
{
    /// <summary>
    ///     the logger.
    /// </summary>
    internal partial class Logger
    {
        internal Logger(Config config, NoticeCompletePutDelegate noticeCompletePut, CompletePutNoticeType completePutNoticeType)
        {
            Config = config ?? throw new NoNullAllowedException(nameof(config));

            if (completePutNoticeType != CompletePutNoticeType.None)
            {
                if (noticeCompletePut == null)
                {
                    throw new NoNullAllowedException(nameof(CompletePutDelegate));
                }
            }

            CompletePutDelegate = noticeCompletePut;
            CompletePutNoticeType = completePutNoticeType;
        }

        internal Logger(String configPath, NoticeCompletePutDelegate noticeCompletePut, CompletePutNoticeType completePutNoticeType)
            : this(Config.Create(configPath), noticeCompletePut, completePutNoticeType)
        {
        }

        ~Logger()
        {
            if (State != StateType.Stop)
            {
                // Finalizer에서 Stop을 불러도 되는지 확실하지 않아 경고만 하고 Stop을 부르지 않았다.
                // 일반적으로 로거는 프로그램과 수명을 같이하므로 Stop을 빼먹어도 큰 문제가 되지 않을 것이다.
                Reporter?.Warn($"Logger is not Stopped! - {State.ToString()}", null, "Finalize");
            }
        }
    }
}

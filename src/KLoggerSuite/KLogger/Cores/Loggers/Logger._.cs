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
    }
}

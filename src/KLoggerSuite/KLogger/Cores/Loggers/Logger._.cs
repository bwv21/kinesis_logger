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
        public Logger(Config config, CompletePutDelegate onCompletePut, CompletePutNotifyType completePutNotifyType)
        {
            Config = config ?? throw new NoNullAllowedException(nameof(config));

            if (completePutNotifyType != CompletePutNotifyType.None)
            {
                if (onCompletePut == null)
                {
                    throw new NoNullAllowedException(nameof(CompletePutDelegate));
                }
            }

            CompletePutDelegate = onCompletePut;
            CompletePutNotifyType = completePutNotifyType;
        }

        public Logger(String configPath, CompletePutDelegate onCompletePut, CompletePutNotifyType completePutNotifyType)
            : this(Config.Create(configPath), onCompletePut, completePutNotifyType)
        {
        }
    }
}

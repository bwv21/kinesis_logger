using System;
using System.Collections.Generic;
using KLogger.Configs;
using KLogger.Cores.Components;
using KLogger.Cores.Structures;
using KLogger.Libs;
using KLogger.Libs.Reporters;
using KLogger.Types;

namespace KLogger.Cores.Loggers
{
    /// <summary>
    ///     Logger의 멤버.
    /// </summary>
    internal partial class Logger
    {
#if DEBUG
        internal BuildType BuildType => BuildType.Debug;
#else
        internal BuildType BuildType => BuildType.Release;
#endif

        private readonly Object _lock = new Object();
        private readonly Object _tickLock = new Object();
        private readonly List<NaiveLoopThread> _loggerThreads = new List<NaiveLoopThread>();

        private Int64 _lastWorkTimeMS;
        private QueueMT<Log> _logQueue;
        private SequenceGenerator _sequenceGenerator;

        internal Config Config { get; }

        internal HashSet<String> IgnoreLogTypes { get; private set; }
        internal Boolean UseIgnoreLogType { get; private set; }
        internal DateTime StartTime { get; private set; }
        internal StateType State { get; private set; } = StateType.Stop;
        internal String InstanceID { get; private set; }
        internal Putter Putter { get; private set; }
        internal LogEncoder LogEncoder { get; private set; }
        internal ErrorCounter ErrorCounter { get; private set; }
        internal Reporter Reporter { get; private set; }
        internal Watcher Watcher { get; private set; }
        internal CompletePutNotifier CompletePutNotifier { get; private set; }
        internal ThroughputController ThroughputController { get; private set; }

        internal CompletePutDelegate CompletePutDelegate { get; }
        internal CompletePutNotifyType CompletePutNotifyType { get; }

        internal Int32 ThreadCount => _loggerThreads.Count;
        internal Int32 LogCountInQueue => _logQueue.Count + (ThroughputController?.LogCountInQueue ?? 0);
    }
}

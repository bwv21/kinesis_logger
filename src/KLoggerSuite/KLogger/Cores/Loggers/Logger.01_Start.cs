using System;
using System.Collections.Generic;
using KLogger.Cores.Components;
using KLogger.Cores.Exceptions;
using KLogger.Cores.Structures;
using KLogger.Libs;
using KLogger.Libs.Reporters;
using KLogger.Types;

namespace KLogger.Cores.Loggers
{
    /// <summary>
    ///     Logger의 시작.
    /// </summary>
    internal partial class Logger
    {
        public StartResultType Start()
        {
            try
            {
                StartImpl();

                return StartResultType.Success;
            }
            catch (LoggerStartException loggerStartException)
            {
                return loggerStartException.StartResult;
            }
        }

        private void StartImpl()
        {
            lock (_lock)
            {
                if (Config == null)
                {
                    throw new LoggerStartException(StartResultType.InvalidConfig);
                }

                if (State != StateType.Stop)
                {
                    throw new LoggerStartException(StartResultType.NotStopped);
                }

                State = StateType.Starting;

                StartTime = DateTime.UtcNow;

                GenerateInstanceID();

                CreateLoggerParts();

                CreateLoggerComponents();
                InitializeLoggerComponents();
                StartLoggerComponents();

                CreateAndStartLoggerThread();

                _lastWorkTimeMS = Now.TimestampMS(StartTime);

                State = StateType.Start;
            }

            Reporter.Fatal("*Start Logger!*\n"                               +
                           $"Assembly Version: `{Config.AssemblyVersion}`\n" +
                           $"Git Hash: `{Config.GitHash}`\n"                 +
                           $"Build: `{BuildType}`\n"                         +
                           $"InstanceID: `{InstanceID}`\n"                   +
                           $"```Config\n{Config.ConfigStringPretty}```");
        }

        private void GenerateInstanceID()
        {
            InstanceID = Rand.RandString(13) + Rand.RandInt32(0, 10).ToString() + Rand.RandInt32(0, 10).ToString() + Rand.RandInt32(0, 10).ToString();
        }

        private void CreateLoggerParts()
        {
            CreateSequenceGenerator();
            CreateLogQueue();
            CreateIgnoreLogTypes();
            CreateReporter();
            CreateErrorCounter();
        }

        private void CreateSequenceGenerator()
        {
            _sequenceGenerator = new SequenceGenerator(Now.TimestampSec(StartTime));
        }

        private void CreateLogQueue()
        {
            _logQueue = new QueueMT<Log>(Config.MaxLogQueueSize);
        }

        private void CreateIgnoreLogTypes()
        {
            IgnoreLogTypes = new HashSet<String>(Config.IgnoreLogTypes);
            UseIgnoreLogType = Config.UseIgnoreLogType == 1;
        }

        private void CreateReporter()
        {
            if (Config.ReporterType == ReporterType.None ||
                Config.ReporterType == ReporterType.InvalidMin ||
                Config.ReporterType == ReporterType.InvalidMax)
            {
                Reporter = new SilenceReporter(Config.ReportLevel);
            }
            else if (Config.ReporterType == ReporterType.Debug)
            {
                Reporter = new DebugReporter(Config.ReportLevel);
            }
            else if (Config.ReporterType == ReporterType.Console)
            {
                Reporter = new ConsoleReporter(Config.ReportLevel);
            }
            else if (Config.ReporterType == ReporterType.Slack)
            {
                if (String.IsNullOrEmpty(Config.SlackConfigs.WebhookUrl))
                {
                    throw new LoggerStartException(StartResultType.InvalidSlackWebhookUrl);
                }

                Reporter = new SlackReporter(Config.ReportLevel,
                                             Config.SlackConfigs.WebhookUrl,
                                             BuildSlackUserName(),
                                             Config.SlackConfigs.DebugChannel,
                                             Config.SlackConfigs.InfoChannel,
                                             Config.SlackConfigs.WarnChannel,
                                             Config.SlackConfigs.ErrorChannel,
                                             Config.SlackConfigs.FatalChannel,
                                             Config.SlackConfigs.IconEmoji,
                                             Config.SlackConfigs.UTCAddHour,
                                             Config.SlackConfigs.TryOrderingMessage == 1);
            }
            else
            {
                throw new LoggerStartException(StartResultType.InvalidReporterType);
            }
        }

        private String BuildSlackUserName()
        {
            const Int32 SHOW_LENGTH = 7;
            String userName = $"{Config.SlackConfigs.NamePrefix} {InstanceID.Substring(0, SHOW_LENGTH)}";
            return userName;
        }

        private void CreateErrorCounter()
        {
            if (Reporter == null)
            {
                throw new LoggerStartException(StartResultType.Invalid); // 초기화 실수.
            }

            ErrorCounter = new ErrorCounter(Reporter, Config.MaxSerialErrorCount, Pause);
        }

        private void CreateLoggerComponents()
        {
            CreateWatcher();
            CreateCompletePutNotifier();
            CreateLogEncoder();
            CreateThroughputController();
            CreatePutter();
        }
        
        private void CreateWatcher()
        {
            Watcher = new Watcher();
        }

        private void CreateCompletePutNotifier()
        {
            if (CompletePutDelegate == null || CompletePutNoticeType == CompletePutNoticeType.None)
            {
                CompletePutNotifier = null;
                return;
            }

            CompletePutNotifier = new CompletePutNotifier();
        }

        private void CreateLogEncoder()
        {
            LogEncoder = new LogEncoder();
        }

        private void CreatePutter()
        {
            Putter = new Putter();
        }

        private void CreateThroughputController()
        {
            if (Config.UseThroughputControl != 1)
            {
                ThroughputController = null;
                return;
            }

            ThroughputController = new ThroughputController();
        }

        private void InitializeLoggerComponents()
        {
            InitializeWatcher();
            InitializeCompletePutNotifier();
            InitializeLogEncoder();
            InitializeThroughputController();
            InitializePutter();
        }

        private void InitializeWatcher()
        {
            Watcher.Initialize(this);
        }

        private void InitializeCompletePutNotifier()
        {
            CompletePutNotifier?.Initialize(this, CompletePutDelegate);
        }

        private void InitializeLogEncoder()
        {
            LogEncoder.Initialize(this);
        }

        private void InitializeThroughputController()
        {
            ThroughputController?.Initialize(this);
        }

        private void InitializePutter()
        {
            Putter.Initialize(this);
        }

        private void StartLoggerComponents()
        {
            StartWatcher();
            StartCompletePutNotifier();
            StartThroughputController();
        }

        private void StartWatcher()
        {
            Watcher.Start();
        }

        private void StartCompletePutNotifier()
        {
            CompletePutNotifier?.Start();
        }

        private void StartThroughputController()
        {
            ThroughputController?.Start();
        }

        private void CreateAndStartLoggerThread()
        {
            for (Int32 i = 0; i < Config.WorkThreadCount; ++i)
            {
                var thread = new NaiveLoopThread(Tick, Config.TickIntervalMS, e => ErrorCounter.RaiseError(e), $"{nameof(Tick)}:{i}");
                _loggerThreads.Add(thread);
                thread.Start();
            }
        }
    }
}

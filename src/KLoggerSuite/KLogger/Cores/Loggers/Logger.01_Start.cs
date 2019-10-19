using System;
using System.Collections.Generic;
using KLogger.Cores.Components;
using KLogger.Cores.Exceptions;
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

                InitializeSequenceGenerator();
                InitializeLogQueue();
                InitializeIgnoreLogTypes();
                InitializeReporter();
                InitializeErrorCounter();
                InitializeWatcher();
                InitializeCompletePutNotifier();
                InitializeLogEncoder();
                InitializeThroughputController();
                InitializePutter();

                StartWatcher();
                StartCompletePutNotifier();
                StartThroughputController();
                CreateAndStartTickThread();

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

        private void InitializeSequenceGenerator()
        {
            _sequenceGenerator = new SequenceGenerator(Now.TimestampSec(StartTime));
        }

        private void InitializeLogQueue()
        {
            _logQueue = new QueueMT<Log>(Config.MaxLogQueueSize);
        }

        private void InitializeIgnoreLogTypes()
        {
            IgnoreLogTypes = new HashSet<String>(Config.IgnoreLogTypes);
            UseIgnoreLogType = Config.UseIgnoreLogType == 1;
        }

        private void InitializeReporter()
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

        private void InitializeErrorCounter()
        {
            if (Reporter == null)
            {
                throw new LoggerStartException(StartResultType.Invalid); // 초기화 실수.
            }

            ErrorCounter = new ErrorCounter(Reporter, Config.MaxSerialErrorCount, Pause);
        }

        private void InitializeWatcher()
        {
            Watcher = new Watcher();
            Watcher.Initialize(this);
        }

        private void InitializeCompletePutNotifier()
        {
            if (ErrorCounter == null)
            {
                throw new LoggerStartException(StartResultType.Invalid); // 초기화 실수.
            }

            CompletePutNotifier = new CompletePutNotifier(this, CompletePutDelegate);
        }

        private void InitializeLogEncoder()
        {
            if (ErrorCounter == null)
            {
                throw new LoggerStartException(StartResultType.Invalid); // 초기화 실수.
            }

            LogEncoder = new LogEncoder(this);
        }

        private void InitializeThroughputController()
        {
            if (Config.UseThroughputControl != 1)
            {
                return;
            }

            if (ErrorCounter == null)
            {
                throw new LoggerStartException(StartResultType.Invalid); // 초기화 실수.
            }

            // Putter와 초기화 순서의 순환 문제가 생겨서 Action 을 사용한다.
            ThroughputController = new ThroughputController(putLog => Putter.Put(putLog), this);
        }

        private void InitializePutter()
        {
            if (Reporter == null || Watcher == null || ErrorCounter == null || CompletePutNotifier == null)
            {
                throw new LoggerStartException(StartResultType.Invalid);    // 초기화 실수.
            }

            if (Config.UseThroughputControl == 1 && ThroughputController == null)
            {
                throw new LoggerStartException(StartResultType.Invalid); // 초기화 실수.
            }

            Putter = new Putter(this);
        }

        private void StartWatcher()
        {
            Watcher.Start();
        }

        private void StartCompletePutNotifier()
        {
            CompletePutNotifier.Start();
        }

        private void StartThroughputController()
        {
            ThroughputController?.Start();
        }

        private void CreateAndStartTickThread()
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

using System;
using KLogger.Types;

namespace KLogger.Libs.Reporters
{
    internal class SlackReporter : Reporter
    {
        private const Int32 THREAD_INTERVAL_MS = 50;

        private readonly SlackWebhook _slackWebhook;
        private readonly String _channelDebug;
        private readonly String _channelInfo;
        private readonly String _channelWarn;
        private readonly String _channelError;
        private readonly String _channelFatal;

        private readonly Boolean _tryOrderingReport;

        private NaiveLoopThread _thread;
        private QueueMT<Action> _reportActions;

        public SlackReporter(ReportLevelType reportLevel,
                             String webhookURL,
                             String userName,
                             String channelDebug,
                             String channelInfo,
                             String channelWarn,
                             String channelError,
                             String channelFatal,
                             String iconEmoji = null,
                             Int32 addUTCHour = 0,
                             Boolean tryOrderingReport = false)
            : base(ReporterType.Slack, reportLevel)
        {
            if (String.IsNullOrEmpty(webhookURL))
            {
                _slackWebhook = null;
            }
            else
            {
                _channelDebug = channelDebug;
                _channelInfo = channelInfo;
                _channelWarn = channelWarn;
                _channelError = channelError;
                _channelFatal = channelFatal;

                _slackWebhook = new SlackWebhook(webhookURL, _channelDebug, userName, iconEmoji, addUTCHour);

                _tryOrderingReport = tryOrderingReport;

                if (_tryOrderingReport)
                {
                    _thread = new NaiveLoopThread(SendReportInQueue, THREAD_INTERVAL_MS, null, nameof(SlackReporter));
                    _reportActions = new QueueMT<Action>();
                    _thread.Start();
                }
                else
                {
                    _thread = null;
                    _reportActions = null;
                }
            }
        }

        ~SlackReporter()
        {
            if (_thread != null)
            {
                _slackWebhook?.SendSync(_channelWarn, nameof(SlackReporter), "Finalize-Warning", $"Missing {nameof(CleanupAndWaitForAsyncSend)}()", "warning", null, true);
                _thread?.Stop();
            }
        }

        private void SendReportInQueue()
        {
            // 큐를 비울 때까지 연속으로 보내면, Sync라 할지라도 순서가 뒤바뀔 가능성이 높아서 하나씩 보낸다.
            // 초당 1000/THREAD_INTERVAL_MS 개의 제한이 생긴다.
            if (_reportActions?.IsEmpty() == false)
            {
                _reportActions?.Pop()();
            }
        }

        protected override void DebugImpl(String text, String userName, String title)
        {
            if (_channelDebug == null)
            {
                return;
            }

            if (_tryOrderingReport)
            {
                _reportActions?.Push(() => _slackWebhook?.SendSync(_channelDebug, userName, title, text, "good", null));
            }
            else
            {
                _slackWebhook?.SendAsync(_channelDebug, userName, title, text, "good", null);
            }
        }

        protected override void InfoImpl(String text, String userName, String title)
        {
            if (_channelInfo == null)
            {
                return;
            }

            if (_tryOrderingReport)
            {
                _reportActions?.Push(() => _slackWebhook?.SendSync(_channelInfo, userName, title, text, "#439FE0", null));
            }
            else
            {
                _slackWebhook?.SendAsync(_channelInfo, userName, title, text, "#439FE0", null);
            }
        }

        protected override void WarnImpl(String text, String userName, String title)
        {
            if (_channelWarn == null)
            {
                return;
            }

            if (_tryOrderingReport)
            {
                _reportActions?.Push(() => _slackWebhook?.SendSync(_channelWarn, userName, title, text, "warning", null));
            }
            else
            {
                _slackWebhook?.SendAsync(_channelWarn, userName, title, text, "warning", null);
            }
        }

        protected override void ErrorImpl(String text, String userName, String title)
        {
            if (_channelError == null)
            {
                return;
            }

            if (_tryOrderingReport)
            {
                _reportActions?.Push(() => _slackWebhook?.SendSync(_channelError, userName, title, text, "danger", null));
            }
            else
            {
                _slackWebhook?.SendAsync(_channelError, userName, title, text, "danger", null);
            }
        }

        protected override void FatalImpl(String text, String userName, String title)
        {
            if (_channelFatal == null)
            {
                return;
            }

            if (_tryOrderingReport)
            {
                _reportActions?.Push(() => _slackWebhook?.SendSync(_channelFatal, userName, title, text, "#000000", null));
            }
            else
            {
                _slackWebhook?.SendAsync(_channelFatal, userName, title, text, "#000000", null);
            }
        }

        public Boolean CleanupAndWaitForAsyncSend(Int32 waitMS)
        {
            _thread?.Stop();

            while (_reportActions.IsEmpty() == false)
            {
                SendReportInQueue();
            }

            _thread = null;
            _reportActions = null;

            return _slackWebhook.WaitForAsyncSend(waitMS);
        }

        public Int32 SendingCount()
        {
            return _slackWebhook?.SendingCount ?? -1;
        }
    }
}

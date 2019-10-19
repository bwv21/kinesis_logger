using System;
using KLogger.Types;

namespace KLogger.Libs.Reporters
{
    internal class DebugReporter : Reporter
    {
        public DebugReporter(ReportLevelType reportLevel)
            : base(ReporterType.Debug, reportLevel)
        {
        }

        protected override void DebugImpl(String text, String userName, String title)
        {
            userName = String.IsNullOrEmpty(userName) ? String.Empty : $"[{userName}]";
            DebugLog.Log($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Debug]{userName} {EscapeSlackFormatting(text)}", title);
        }

        protected override void InfoImpl(String text, String userName, String title)
        {
            userName = String.IsNullOrEmpty(userName) ? String.Empty : $"[{userName}]";
            DebugLog.Log($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Info]{userName} {EscapeSlackFormatting(text)}", title);
        }

        protected override void WarnImpl(String text, String userName, String title)
        {
            userName = String.IsNullOrEmpty(userName) ? String.Empty : $"[{userName}]";
            DebugLog.Log($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Warn]{userName} {EscapeSlackFormatting(text)}", title);
        }

        protected override void ErrorImpl(String text, String userName, String title)
        {
            userName = String.IsNullOrEmpty(userName) ? String.Empty : $"[{userName}]";
            DebugLog.Log($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Error]{userName} {EscapeSlackFormatting(text)}", title);
        }

        protected override void FatalImpl(String text, String userName, String title)
        {
            userName = String.IsNullOrEmpty(userName) ? String.Empty : $"[{userName}]";
            DebugLog.Log($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Fatal]{userName} {EscapeSlackFormatting(text)}", title);
        }
    }
}

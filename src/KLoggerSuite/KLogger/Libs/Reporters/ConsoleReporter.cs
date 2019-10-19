using System;
using KLogger.Types;

namespace KLogger.Libs.Reporters
{
    internal class ConsoleReporter : Reporter
    {
        public ConsoleReporter(ReportLevelType reportLevel)
            : base(ReporterType.Console, reportLevel)
        {
        }

        protected override void DebugImpl(String text, String userName, String title)
        {
            userName = String.IsNullOrEmpty(userName) ? String.Empty : $"[{userName}]";
            title = String.IsNullOrEmpty(title) ? String.Empty : $"[{title}]";
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Debug]{userName}{title} {EscapeSlackFormatting(text)}");
        }

        protected override void InfoImpl(String text, String userName, String title)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Info]{userName}{title} {EscapeSlackFormatting(text)}");
        }

        protected override void WarnImpl(String text, String userName, String title)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Warn]{userName}{title} {EscapeSlackFormatting(text)}");
        }

        protected override void ErrorImpl(String text, String userName, String title)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Error]{userName}{title} {EscapeSlackFormatting(text)}");
        }

        protected override void FatalImpl(String text, String userName, String title)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][Fatal]{userName}{title} {EscapeSlackFormatting(text)}");
        }
    }
}

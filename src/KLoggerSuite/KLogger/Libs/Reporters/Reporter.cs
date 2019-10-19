using System;
using System.Text;
using KLogger.Types;

namespace KLogger.Libs.Reporters
{
    internal abstract class Reporter
    {
        public ReporterType ReporterType { get; private set; }
        public ReportLevelType ReportLevel { get; private set; }

        protected Reporter(ReporterType reporterType, ReportLevelType reportLevel)
        {
            ReporterType = reporterType;
            ReportLevel = reportLevel;
        }

        public void Debug(String text, String userName = null, String title = null)
        {
            if (ReportLevelType.Debug < ReportLevel)
            {
                return;
            }

            DebugImpl(text, userName, title);
        }

        public void Info(String text, String userName = null, String title = null)
        {
            if (ReportLevelType.Info < ReportLevel)
            {
                return;
            }

            InfoImpl(text, userName, title);
        }

        public void Warn(String text, String userName = null, String title = null)
        {
            if (ReportLevelType.Warn < ReportLevel)
            {
                return;
            }

            WarnImpl(text, userName, title);
        }

        public void Error(String text, String userName = null, String title = null)
        {
            if (ReportLevelType.Error < ReportLevel)
            {
                return;
            }

            ErrorImpl(text, userName, title);
        }

        public void Fatal(String text, String userName = null, String title = null)
        {
            if (ReportLevelType.Fatal < ReportLevel)
            {
                return;
            }

            FatalImpl(text, userName, title);
        }

        protected String EscapeSlackFormatting(String text)
        {
            // 엄격한 룰을 사용하고 있지 않음.
            var stringBuilder = new StringBuilder(text);
            stringBuilder.Replace("`", String.Empty);

            return stringBuilder.ToString();
        }

        protected abstract void DebugImpl(String text, String userName, String title);
        protected abstract void InfoImpl(String text, String userName, String title);
        protected abstract void WarnImpl(String text, String userName, String title);
        protected abstract void ErrorImpl(String text, String userName, String title);
        protected abstract void FatalImpl(String text, String userName, String title);
    }
}

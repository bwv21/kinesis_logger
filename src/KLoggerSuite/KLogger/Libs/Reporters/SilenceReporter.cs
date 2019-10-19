using System;
using KLogger.Types;

namespace KLogger.Libs.Reporters
{
    internal class SilenceReporter : Reporter
    {
        public SilenceReporter(ReportLevelType reportLevel)
            : base(ReporterType.None, reportLevel)
        {
        }

        protected override void DebugImpl(String text, String userName, String title)
        {}

        protected override void InfoImpl(String text, String userName, String title)
        {}

        protected override void WarnImpl(String text, String userName, String title)
        {}

        protected override void ErrorImpl(String text, String userName, String title)
        {}

        protected override void FatalImpl(String text, String userName, String title)
        {}
    }
}

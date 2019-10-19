using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace KLogger.Libs
{
    internal static class DebugLog
    {
        [Conditional("DEBUG")]
        public static void Log(String s, String keyword = "default", Boolean removeBreakLine = false, [CallerFilePath]String sourceFilePath = "", [CallerLineNumber]Int32 sourceLineNumber = 0)
        {
            if (removeBreakLine)
            {
                s = s.Replace(Environment.NewLine, " ");
            }

            Debug.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}][{keyword}][{Thread.CurrentThread.ManagedThreadId.ToString()}] {s} ({Path.GetFileName(sourceFilePath)}:{ sourceLineNumber.ToString()})");
        }
    }
}

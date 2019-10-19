using System;
using System.Linq;

namespace KLogger.Libs
{
    internal static class Extensions
    {
        public static String HideString(this String s, Int32 showLength, Boolean showLastChar = false, Char hideChar = '*')
        {
            if (s.Length <= showLength)
            {
                return s;
            }

            if (showLength <= 0)
            {
                return new String(Enumerable.Repeat(hideChar, s.Length).ToArray());
            }

            Char lastChar = s[s.Length - 1];

            String hideString = s.Substring(0, showLength).PadRight(s.Length, '*');

            if (showLastChar)
            {
                hideString = hideString.Remove(hideString.Length - 1) + lastChar;
            }

            return hideString;
        }
    }
}

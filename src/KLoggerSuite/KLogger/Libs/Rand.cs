using System;
using System.Diagnostics;
using System.Linq;

namespace KLogger.Libs
{
    internal static class Rand
    {
        private static readonly Random Random;
        private static readonly String Chars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // 0, o, O, 1, l 없음.

        static Rand()
        {
            Int32 seed = MakeSeed();
            Random = new Random(seed);

            DebugLog.Log($"random seed: {seed}", "klogger:rand");
        }

        public static String RandString(Int32 length)
        {
            return new String(Enumerable.Repeat(Chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        public static Int32 RandInt32(Int32 min, Int32 max)
        {
            return Random.Next(min, max);
        }

        // 무작위 시드를 만들기 위해서 CPU카운터 값들을 조합한다.
        private static Int32 MakeSeed()
        {
#if DEBUG
            return 0;
#endif

            Int64 seed = Environment.TickCount;

            unchecked
            {
                try
                {
                    PerformanceCounterCategory[] categories = PerformanceCounterCategory.GetCategories();
                    foreach (PerformanceCounterCategory category in categories)
                    {
                        if (category.CategoryName != "Processor")
                        {
                            continue;
                        }

                        foreach (PerformanceCounter performanceCounter in category.GetCounters("_Total"))
                        {
                            if (performanceCounter.RawValue == 0)
                            {
                                seed += performanceCounter.RawValue;
                            }
                            else
                            {
                                seed *= performanceCounter.RawValue;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // ignored.
                }

                return (Int32)seed;
            }
        }
    }
}

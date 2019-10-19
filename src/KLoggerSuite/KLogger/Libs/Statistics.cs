using System;
using System.Collections.Generic;

namespace KLogger.Libs
{
    internal static class Statistics
    {
        public struct MVS
        {
            public Double Mean; // 평균.
            public Double Var;  // 분산.
            public Double SD;   // 표준편차.
        }

        public static MVS CalculateMVS(IReadOnlyList<Int32> datas, Int32 round = 3)
        {
            if (datas.Count <= 0)
            {
                return new MVS();
            }

            Int32 sum = 0;
            Int32 squaredSum = 0;
            Int32 count = 0;
            foreach (Int32 data in datas)
            {
                sum += data;
                squaredSum += data * data;
                ++count;
            }

            Double mean = (Double)sum / count;
            Double squaredMean = (Double)squaredSum / count;
            Double variance = squaredMean - (mean * mean);

            return new MVS
                   {
                       Mean = Math.Round(mean, round),
                       Var = Math.Round(variance, round),
                       SD = Math.Round(Math.Sqrt(variance), round)
                   };
        }
    }
}

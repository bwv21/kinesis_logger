using System;
using System.Threading;

namespace KLogger.Libs
{
    internal class SequenceGenerator
    {
        private Int64 _sequence;

        public SequenceGenerator(Int64 initialSequence = 0)
        {
            _sequence = initialSequence;
        }

        public Int64 Generate()
        {
            Interlocked.CompareExchange(ref _sequence, -1, Int64.MaxValue);
            return Interlocked.Increment(ref _sequence);
        }
    }
}

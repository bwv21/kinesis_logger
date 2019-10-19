using System;
using System.Collections.Concurrent;

namespace KLogger.Libs
{
    internal class QueueMT<T> 
        where T : class
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        public Int32 MaxQueueSize { get; }
        public Int32 Count => _queue.Count;

        public QueueMT(Int32 maxQueueSize = Int32.MaxValue)
        {
            MaxQueueSize = maxQueueSize;
        }

        public Boolean Push(T item)
        {
            if (MaxQueueSize <= _queue.Count)
            {
                return false;
            }

            _queue.Enqueue(item);

            return true;
        }

        public T Pop()
        {
            return _queue.TryDequeue(out T item) ? item : null;
        }

        public T Peek()
        {
            return _queue.TryPeek(out T item) ? item : null;
        }

        public Boolean IsEmpty()
        {
            return _queue.IsEmpty;
        }

        public void Clear()
        {
            while (_queue.IsEmpty == false)
            {
                _queue.TryDequeue(out T _);
            }
        }
    }
}

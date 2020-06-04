using System;
using System.Threading;
using Backup4.Functional;

namespace Backup4.Synchronization
{
    public class ConcurrentBuffer<T>
    {
        private readonly T[] _buffer;
        private readonly SemaphoreSlim _filledCount;
        private readonly SemaphoreSlim _emptyCount;
        private readonly object _bufferFrontMutex;
        private readonly object _bufferBackMutex;
        private int _producerPos;
        private int _consumerPos;

        public ConcurrentBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _filledCount = new SemaphoreSlim(0, capacity);
            _emptyCount = new SemaphoreSlim(capacity, capacity);
            _bufferFrontMutex = new object();
            _bufferBackMutex = new object();
            _producerPos = 0;
            _consumerPos = 0;
            Capacity = capacity;
        }

        public int Length => _filledCount.CurrentCount;

        public int Capacity { get; }

        public void Push(T elem)
        {
            _emptyCount.Wait();
            lock (_bufferBackMutex)
            {
                _buffer[_producerPos] = elem;
                _producerPos = (_producerPos + 1) % _buffer.Length;
                _filledCount.Release();
            }
        }

        public bool Push(T elem, CancellationToken token)
        {
            try
            {
                _emptyCount.Wait(token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            lock (_bufferBackMutex)
            {
                _buffer[_producerPos] = elem;
                _producerPos = (_producerPos + 1) % _buffer.Length;
            }

            _filledCount.Release();
            return true;
        }

        public T Pop()
        {
            _filledCount.Wait();

            T ret;
            lock (_bufferFrontMutex)
            {
                ret = _buffer[_consumerPos];
                _consumerPos = (_consumerPos + 1) % _buffer.Length;
            }

            _emptyCount.Release();
            return ret;
        }

        public Option<T> Pop(CancellationToken token)
        {
            try
            {
                _filledCount.Wait(token);
            }
            catch (OperationCanceledException)
            {
                return Option<T>.Empty;
            }

            T ret;
            lock (_bufferFrontMutex)
            {
                ret = _buffer[_consumerPos];
                _consumerPos = (_consumerPos + 1) % _buffer.Length;
            }

            _emptyCount.Release();
            return ret!;
        }
    }
}
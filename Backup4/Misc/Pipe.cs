using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Backup4.Misc
{
    internal class PipeBuffer<T>
    {
        private readonly T[] _buffer;
        private int _inPtr;
        private int _outPtr;
        private readonly SemaphoreSlim _filledCount;
        private readonly SemaphoreSlim _emptyCount;

        public PipeBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _inPtr = 0;
            _outPtr = 0;
            _filledCount = new SemaphoreSlim(0, capacity);
            _emptyCount = new SemaphoreSlim(capacity, capacity);
        }

        public int Count => _filledCount.CurrentCount;

        public void Push(T val)
        {
            _emptyCount.Wait();
            _buffer[_inPtr] = val;
            _inPtr = (_inPtr + 1) % _buffer.Length;
            _filledCount.Release();
        }

        public async Task PushAsync(T val)
        {
            await _emptyCount.WaitAsync();
            _buffer[_inPtr] = val;
            _inPtr = (_inPtr + 1) % _buffer.Length;
            _filledCount.Release();
        }

        public T Pop()
        {
            _filledCount.Wait();
            var tmp = _buffer[_outPtr];
            _outPtr = (_outPtr + 1) % _buffer.Length;
            _emptyCount.Release();
            return tmp;
        }

        public async Task<T> PopAsync()
        {
            await _filledCount.WaitAsync();
            var tmp = _buffer[_outPtr];
            _outPtr = (_outPtr + 1) % _buffer.Length;
            _emptyCount.Release();
            return tmp;
        }
    }

    public class Pipe<TIn, TOut> : IEnumerable<TOut>
    {
        private readonly PipeBuffer<TIn> _buffer;
        private readonly IEnumerable<TIn> _producerEnumerable;
        private readonly IEnumerable<TOut> _consumerEnumerable;
        private readonly Thread _producer;
        private bool _done = false;

        private void _producerFunc()
        {
            foreach (var thing in _producerEnumerable)
            {
                _buffer.Push(thing);
            }
            _done = true;
        }

        private IEnumerable<TIn> _consumerFunc()
        {
            while (!_done || _buffer.Count > 0)
            {
                yield return _buffer.Pop();
            }
        }

        public Pipe(IEnumerable<TIn> producer, Func<IEnumerable<TIn>, IEnumerable<TOut>> consumer, int bufferCapacity = 1024)
        {
            _buffer = new PipeBuffer<TIn>(bufferCapacity);
            _producerEnumerable = producer;
            
            _producer = new Thread(_producerFunc);
            _producer.Start();
            
            _consumerEnumerable = consumer(_consumerFunc());
        }

        public void Join()
        {
            _producer.Join();
        }

        public IEnumerator<TOut> GetEnumerator()
        {
            return _consumerEnumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
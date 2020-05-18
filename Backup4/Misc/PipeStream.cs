using System;
using System.IO;
using System.Threading;

namespace Backup4.Misc
{
    public class PipeStream : Stream
    {
        public static (PipeStream Input, PipeStream Output) Connect(int capacity, params Action<Stream, Stream>[] funcs)
        {
            if (funcs.Length == 0)
            {
                throw new ArgumentException("funcs must have at least one function");
            }
            
            var origInput = new PipeStream(capacity);
            
            var input = origInput;
            var output = origInput;
            
            foreach (var func in funcs)
            {
                var ip = input;
                var op = output;
                
                new Thread(() =>
                {
                    func(ip, op);
                    ip.Done = true;
                }).Start();

                input = op;
                output = new PipeStream(capacity);
            }

            return (origInput, output);
        }
    
        private readonly byte[] _buffer;
        private int _beginPos;
        private int _endPos;

        private int _capacityRequested;
        private readonly ManualResetEventSlim _waitingForInput;
        private readonly ManualResetEventSlim _notEnoughCapacity;
        private bool _done;

        public PipeStream(int capacity = 4 * 1024 * 1024)
        {
            _buffer = new byte[capacity + 1];
            _beginPos = 0;
            _endPos = 0;
            _capacityRequested = 0;
            _notEnoughCapacity = new ManualResetEventSlim();
            _waitingForInput = new ManualResetEventSlim();
        }

        public int Capacity => _buffer.Length - 1;

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > _buffer.Length)
            {
                throw new ArgumentException(
                    $"The requested count {count} is greater than the capacity {Capacity} of this pipe.");
            }

            int len;
            lock (_lengthLock)
            {
                len = (int) Length;
            }

            if (len == 0)
            {
                if (_done)
                {
                    return 0;
                }

                _waitingForInput.Reset();
                _waitingForInput.Wait();

                if (_done)
                {
                    return 0;
                }

                lock (_lengthLock)
                {
                    len = (int) Length;
                }
            }

            count = Math.Min(count, len);

            if (count <= _buffer.Length - _beginPos)
            {
                Array.Copy(_buffer, _beginPos, buffer, offset, count);
            }
            else
            {
                var lenToEnd = _buffer.Length - _beginPos;
                var lenFromBeginning = count - lenToEnd;
                var divider = lenToEnd + offset;

                Array.Copy(_buffer, _beginPos, buffer, offset, lenToEnd);
                Array.Copy(_buffer, 0, buffer, divider, lenFromBeginning);
            }


            lock (_lengthLock)
            {
                _beginPos = (_beginPos + count) % _buffer.Length;
                len = (int) Length;
            }

            if (_buffer.Length - len - 1 >= _capacityRequested)
            {
                _notEnoughCapacity.Set();
            }

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > _buffer.Length)
            {
                throw new ArgumentException(
                    $"The requested count {count} is greater than the capacity {Capacity} of this pipe.");
            }

            int len;
            lock (_lengthLock)
            {
                len = (int) Length;
            }

            if (count > _buffer.Length - len - 1)
            {
                if (!_done)
                {
                    _notEnoughCapacity.Reset();
                }
                _capacityRequested = count;
                _notEnoughCapacity.Wait();
            }

            if (count <= _buffer.Length - _endPos)
            {
                Array.Copy(buffer, offset, _buffer, _endPos, count);
            }
            else
            {
                var lenToEnd = _buffer.Length - _endPos;
                var lenFromBeginning = count - lenToEnd;
                var divider = lenToEnd + offset;

                Array.Copy(buffer, offset, _buffer, _endPos, lenToEnd);
                Array.Copy(buffer, divider, _buffer, 0, lenFromBeginning);
            }

            lock (_lengthLock)
            {
                _endPos = (_endPos + count) % _buffer.Length;
            }
            _waitingForInput.Set();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        private readonly object _lengthLock = new object();

        public override long Length =>
            _endPos >= _beginPos
                ? _endPos - _beginPos
                : _buffer.Length - _beginPos + _endPos;


        public override long Position { get; set; }

        public bool Done
        {
            get => _done;
            set
            {
                if (!value)
                {
                    return;
                }
                _done = true;
                _waitingForInput.Set();
                _notEnoughCapacity.Set();
            }
        }
    }
}
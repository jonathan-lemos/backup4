using System;
using System.IO;
using System.Threading;

namespace Backup4.Misc
{
    public class PipeStream : Stream
    {
        private readonly byte[] _buffer;
        private readonly object _lengthMtx;
        private int _begin;
        private int _end;
        private readonly int _capacity;
        private bool _full;
        private readonly AutoResetEvent _readerWaiter;
        private readonly AutoResetEvent _writerWaiter;
        private bool _writerClosed;
        private bool _readerClosed;
        private int _emptyRequested;

        public PipeStream(int capacity = 64 * 1024)
        {
            if (capacity < 8)
            {
                throw new ArgumentException("PipeBuffer capacity must be >= 8");
            }

            _capacity = capacity;
            _buffer = new byte[capacity];
            _lengthMtx = new object();
            _begin = 0;
            _end = 0;
            _full = false;
            _readerWaiter = new AutoResetEvent(false);
            _writerWaiter = new AutoResetEvent(false);
            _writerClosed = false;
            _readerClosed = false;
        }

        public void CloseForWriting()
        {
            _writerClosed = true;
            _writerWaiter.Set();
            _readerWaiter.Set();
        }

        public void CloseForReading()
        {
            _readerClosed = true;
            _writerWaiter.Set();
            _readerWaiter.Set();
        }

        private void _requestWrite(int count)
        {
            lock (_lengthMtx)
            {
                if (_writerClosed)
                {
                    throw new InvalidOperationException("Cannot write when the PipeStream is closed for writing.");
                }

                if (_readerClosed)
                {
                    return;
                }

                if (_capacity - _lengthNonBlocking() < count)
                {
                    _emptyRequested = count;
                }
                else
                {
                    return;
                }
            }
            _writerWaiter.WaitOne();
        }

        private void _requestRead()
        {
            lock (_lengthMtx)
            {
                if (_writerClosed)
                {
                    return;
                }

                if (_readerClosed)
                {
                    throw new InvalidOperationException("Cannot read when the PipeStream is closed for reading.");
                }

                if (_lengthNonBlocking() > 0)
                {
                    return;
                }
            }
            _readerWaiter.WaitOne();
        }

        private void _setBegin(Func<int, int> func)
        {
            lock (_lengthMtx)
            {
                _begin = func(_begin);
                if (_begin == _end)
                {
                    _full = false;
                }

                if (_lengthNonBlocking() < _capacity - _emptyRequested)
                {
                    _writerWaiter.Set();
                }
            }
        }

        private void _setEnd(Func<int, int> func)
        {
            lock (_lengthMtx)
            {
                _end = func(_end);
                if (_end == _begin)
                {
                    _full = true;
                }

                _readerWaiter.Set();
            }
        }

        private long _lengthNonBlocking()
        {
            if (_end == _begin)
            {
                return _full ? _capacity : 0;
            }

            if (_end > _begin)
            {
                return _end - _begin;
            }

            return _end + (_capacity - _begin);
        }

        public override long Length
        {
            get
            {
                lock (_lengthMtx)
                {
                    return _lengthNonBlocking();
                }
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > _capacity)
            {
                var total = 0;
                var interval = _capacity / 2;
                for (var i = 0; i < count; ++i)
                {
                    var res = Read(buffer, offset + i, Math.Min(interval, count - i));
                    if (res == 0)
                    {
                        break;
                    }

                    total += res;
                }

                return total;
            }
            
            _requestRead();
            
            count = Math.Min(count, (int) Length);

            if (count <= _buffer.Length - _begin)
            {
                Array.Copy(_buffer, _begin, buffer, offset, count);
            }
            else
            {
                var lenToEnd = _buffer.Length - _begin;
                var lenFromBeginning = count - lenToEnd;
                var divider = lenToEnd + offset;

                Array.Copy(_buffer, _begin, buffer, offset, lenToEnd);
                Array.Copy(_buffer, 0, buffer, divider, lenFromBeginning);
            }


            _setBegin(e => (e + count) % _capacity);
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
            if (count > _capacity)
            {
                var interval = _capacity / 2;
                for (var i = 0; i < count; i += interval)
                {
                    Write(buffer, i + offset, Math.Min(interval, count - i));
                }
                return;
            }
            
            _requestWrite(count);
            
            if (count <= _capacity - _end)
            {
                Array.Copy(buffer, offset, _buffer, _end, count);
            }
            else
            {
                var lenToEnd = _buffer.Length - _end;
                var lenFromBeginning = count - lenToEnd;
                var divider = lenToEnd + offset;

                Array.Copy(buffer, offset, _buffer, _end, lenToEnd);
                Array.Copy(buffer, divider, _buffer, 0, lenFromBeginning);
            }
            
            _setEnd(e => (e + count) % _capacity);
        }

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = true;
        public override long Position { get; set; } = 0;
    }
}
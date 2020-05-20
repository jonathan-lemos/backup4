using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Backup4.Synchronization;

namespace Backup4.Misc
{
    public class PipeStream : Stream
    {
        public static async Task Connect(Stream input, Stream output, int capacity,
            params Action<Stream, Stream>[] funcs)
        {
            if (funcs.Length == 0)
            {
                throw new ArgumentException("funcs must have at least one function");
            }

            var psiOrig = new PipeStream(capacity);

            var psi = psiOrig;
            var pso = new PipeStream(capacity);

            var tasks = new List<Task>();

            foreach (var func in funcs)
            {
                var ip = psi;
                var op = pso;

                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        func(ip, op);
                    }
                    finally
                    {
                        op.NoMoreInput = true;
                    }
                }));

                psi = op;
                pso = new PipeStream(capacity);
            }

            tasks.AddRange(new[]
            {
                Task.Run(() =>
                {
                    input.CopyToIncremental(psiOrig);
                    psiOrig.NoMoreInput = true;
                }),
                Task.Run(() => psi.CopyToIncremental(output))
            });

            await Task.WhenAll(tasks);
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
            _lengthLock = new object();
        }

        public int Capacity => _buffer.Length - 1;

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > _buffer.Length)
            {
                var interval = Math.Max(Capacity / 2, 1);
                for (; offset < count; offset += interval)
                {
                    Read(buffer, offset, interval);
                }

                return count;
            }

            int len;
            using (var _ = new Lock(_lengthLock))
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

                using (var _ = new Lock(_lengthLock))
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


            using (var _ = new Lock(_lengthLock))
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
                var interval = Math.Max(Capacity / 2, 1);
                for (; offset < count; offset += interval)
                {
                    Write(buffer, offset, interval);
                }

                return;
            }

            int len;
            using (var _ = new Lock(_lengthLock))
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

            using (var _ = new Lock(_lengthLock))
            {
                _endPos = (_endPos + count) % _buffer.Length;
            }

            _waitingForInput.Set();
            Position += count;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        private readonly object _lengthLock;

        public override long Length =>
            _endPos >= _beginPos
                ? _endPos - _beginPos
                : _buffer.Length - _beginPos + _endPos;


        public override long Position { get; set; } = 0;

        public bool NoMoreInput
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
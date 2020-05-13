using System;
using System.Collections.Generic;
using System.IO;

namespace Backup4.Misc
{
    public class EnumerableStream : Stream
    {
        public static IEnumerable<byte[]> StreamToEnumerable(Stream stream, int chunkLen = 65536)
        {
            var res = 0;
            var buf = new byte[chunkLen];
            while ((res = stream.Read(buf, 0, buf.Length)) > 0)
            {
                if (res == chunkLen)
                {
                    yield return buf;
                }
                else
                {
                    yield return buf[..buf.Length];
                }
            }
        }
        
        private readonly IEnumerator<byte[]> _enumerator;
        private bool _valid;
        private int _currentIndex;
        
        public EnumerableStream(IEnumerable<byte[]> bytes)
        {
            _enumerator = bytes.GetEnumerator();
            _currentIndex = 0;
            _valid = _enumerator.MoveNext();
        }
        
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var countRet = 0;

            while (true)
            {
                if (!_valid)
                {
                    return countRet;
                }

                if (_enumerator.Current.Length - _currentIndex >= count)
                {
                    Array.Copy(_enumerator.Current, _currentIndex, buffer, offset, count);
                    
                    _currentIndex += count;
                    countRet += count;
                    return countRet;
                }

                var len = _enumerator.Current.Length - _currentIndex;
                Array.Copy(_enumerator.Current, _currentIndex, buffer, offset, len);
                
                offset += len;
                countRet += len;

                _valid = _enumerator.MoveNext();
            }
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
            throw new System.NotImplementedException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => long.MaxValue;
        public override long Position { get; set; }
    }
}
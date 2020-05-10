using System;
using System.Collections.Generic;
using System.Linq;

namespace Backup4.Misc
{
    public static class ByteStream
    {
        public static (byte[] Bytes, byte[] Leftover) GetBytes(IEnumerator<byte[]> enumerator, int len)
        {
            return GetBytes(enumerator, new byte[] { }, len);
        }

        public static (byte[] Bytes, byte[] Leftover) GetBytes(IEnumerator<byte[]> enumerator, byte[] leftover, int len)
        {
            while (true)
            {
                if (len < 0)
                {
                    throw new ArgumentException("Length must be >= 0");
                }

                if (leftover.Length >= len)
                {
                    return (leftover.Slice(0, len).ToArray(), leftover.Slice(len).ToArray());
                }

                if (!enumerator.MoveNext())
                {
                    return (leftover, new byte[] { });
                }

                leftover = leftover.Concat(enumerator.Current).ToArray();
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}
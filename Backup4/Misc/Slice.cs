using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Backup4.Synchronization
{
    public class Slice : IEnumerable<byte>, IEquatable<Slice>, IEquatable<byte[]>
    {
        private readonly byte[] _bytes;
        private readonly int _begin;
        public int Length { get; }

        public Slice(byte[] bytes, int begin) : this(bytes, begin, bytes.Length - begin)
        {
        }

        public Slice(byte[] bytes, int begin, int length)
        {
            if (begin < 0)
            {
                begin = bytes.Length + begin;
            }

            if (length < 0)
            {
                length = bytes.Length + length;
            }

            if (begin < 0 || length < 0)
            {
                throw new ArgumentException($"begin and/or length must be at least -{bytes.Length}.");
            }

            if (begin >= bytes.Length)
            {
                begin = bytes.Length;
            }

            if (begin + length > bytes.Length)
            {
                length = bytes.Length - begin;
            }

            (_bytes, _begin, Length) = (bytes, begin, Math.Min(length, bytes.Length - _begin));
        }

        public IEnumerable<byte> Bytes()
        {
            for (int i = _begin, j = 0; j < Length; ++i, ++j)
            {
                yield return _bytes[i];
            }
        }

        public IEnumerator<byte> GetEnumerator() => Bytes().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj == null)
            {
                return false;
            }

            if (obj is Slice s)
            {
                return Equals(s);
            }

            if (obj is byte[] b)
            {
                return Equals(b);
            }

            return false;
        }

        public override int GetHashCode()
        {
            var data = Bytes().ToArray();

            unchecked
            {
                const int p = 16777619;
                var hash = (int) 2166136261;

                for (var i = 0; i < data.Length; i++)
                    hash = (hash ^ data[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        public bool Equals(Slice other)
        {
            return this.SequenceEqual(other);
        }

        public bool Equals(byte[] other)
        {
            return this.SequenceEqual(other);
        }

        public static bool operator ==(Slice s1, Slice s2) => s1.Equals(s2);

        public static bool operator !=(Slice s1, Slice s2) => !s1.Equals(s2);

        public static bool operator ==(Slice s1, byte[] s2) => s1.Equals(s2);

        public static bool operator !=(Slice s1, byte[] s2) => !s1.Equals(s2);

        public static bool operator ==(byte[] s1, Slice s2) => s2.Equals(s1);

        public static bool operator !=(byte[] s1, Slice s2) => !s2.Equals(s1);
    }
}
using System;
using System.Collections.Generic;
using System.Text;

namespace Backup4.Synchronization
{
    public static class Extensions
    {
        public static byte[] Truncate(this byte[] b, int len)
        {
            if (len >= b.Length)
            {
                return b;
            }

            if (len < 0)
            {
                throw new ArgumentException("Length cannot be < 0.");
            }
            
            var buf = new byte[len];
            Array.Copy(b, buf, len);
            return buf;
        }
        public static byte[] ToUtf8Bytes(this string s) => Encoding.UTF8.GetBytes(s);
        public static string ToUtf8String(this byte[] b) => Encoding.UTF8.GetString(b);
        
        public static Slice Slice(this byte[] b, int begin) => new Slice(b, begin);
        public static Slice Slice(this byte[] b, int begin, int length) => new Slice(b, begin, length);
    }
}
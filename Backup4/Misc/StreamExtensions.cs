using System.IO;

namespace Backup4.Synchronization
{
    public static class StreamExtensions
    {
        public static byte[] GetBytes(this Stream s, int length)
        {
            var ret = new byte[length];
            var len = s.Read(ret, 0, ret.Length);
            return ret.Truncate(len);
        }

        public static void CopyToIncremental(this Stream s, Stream output, int bufLen = 1 << 16)
        {
            var len = 0;
            var buf = new byte[bufLen];

            while ((len = s.Read(buf, 0, bufLen)) > 0)
            {
                output.Write(buf, 0, len);
            }
        }
    }
}
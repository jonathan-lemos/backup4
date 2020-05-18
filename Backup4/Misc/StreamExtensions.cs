using System.IO;

namespace Backup4.Misc
{
    public static class StreamExtensions
    {
        public static byte[] GetBytes(this Stream s, int length)
        {
            var ret = new byte[length];
            var len = s.Read(ret, 0, ret.Length);
            return ret.Truncate(len);
        }
    }
}
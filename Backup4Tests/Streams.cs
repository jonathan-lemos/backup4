using System.Collections.Generic;
using System.IO;
using Backup4.Misc;

namespace Backup4Tests
{
    public static class StreamExtensions
    {
        public static Stream ToStream(this byte[] bytes) => new MemoryStream(bytes);

        public static byte[] ToBytes(this Stream s)
        {
            var res = new List<byte>();
            var block = new byte[32];
            var len = 0;

            while ((len = s.Read(block, 0, block.Length)) > 0)
            {
                res.AddRange(block.Truncate(len));
            }

            return res.ToArray();
        }
    }
}
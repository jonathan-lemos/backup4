using System;
using System.IO;
using System.Linq;
using System.Text;
using Backup4.Compression;
using Backup4.Misc;
using NUnit.Framework;

namespace Backup4Tests
{
    public class LzmaCompressorTests
    {
        public static string Repeat(string s, int count)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < count; ++i)
            {
                sb.Append(s);
            }

            return sb.ToString();
        }

        [Test]
        public void LzmaCompressionDecompression()
        {
            var compressor = new LzmaCompressor {Level = 9};
            var data = new[] {"abcd", Repeat("xyz", 100)}
                .SelectMany(x => x.ToUtf8Bytes())
                .Concat(Enumerable.Range(0, 1_000_000).Select(x => (byte) x))
                .ToArray();

            var expected = data.ToArray();

            compressor.DecompressLength = data.Length;

            var compStream = new MemoryStream();
            compressor.Compress(data.ToStream(), compStream);
            var compressed = compStream.ToArray();
            
            Assert.Less(compressed.Length, data.Length);

            var decompStream = new MemoryStream();
            compressor.Decompress(compressed.ToStream(), decompStream);
            var decomp = decompStream.ToArray();
                
            Assert.AreEqual(expected, decomp);
        }
    }
}
using System;
using System.Linq;
using System.Text;
using Backup4.Compression;
using Backup4.Misc;
using NUnit.Framework;

namespace Backup4Tests
{
    public class LzmaCompressorTests
    {
        /*
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
            var compressor = new LzmaCompressor(9);
            var data = new[] {"abcd", Repeat("xyz", 100)}.Concat(Enumerable.Range(0, 1000000)
                .Select((x, i) => (Data: x.ToString(), Index: i))
                .GroupBy(x => x.Index / 10000)
                .Select(x => string.Join(", ", x.Select(y => y.Data))))
                .ToList();

            var expected = string.Join("", data);

            var compressed = compressor.Compress(data.Select(x => x.ToUtf8Bytes()))
                .ToList();

            var decompressed = compressor.Decompress(compressed);

            var res = string.Join("", decompressed.Select(x => x.ToUtf8String()));
            
            Assert.AreEqual(expected, res);
        }
        */
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Backup4.Misc;
using NUnit.Framework;

namespace Backup4Tests
{
    public class PipeTests
    {
        [Test]
        public async Task ConnectTest()
        {
            var ip = Enumerable.Range(0, 1028).Select(x => (byte) x).ToArray();

            void TransformBigger(Stream input, Stream output)
            {
                var len = 0;
                var read = new List<byte>();
                var wrote = new List<byte>();
                var buf = new byte[69];
                var zero = new byte[10];

                wrote.AddRange(zero);
                output.Write(zero, 0, zero.Length);
                while ((len = input.Read(buf, 0, buf.Length)) > 0)
                {
                    read.AddRange(buf.Truncate(len));
                    wrote.AddRange(buf.Truncate(len));
                    output.Write(buf, 0, len);
                }

                wrote.AddRange(zero);
                output.Write(zero, 0, zero.Length);
            }

            void TransformSmaller(Stream input, Stream output)
            {
                var len = 0;
                var read = new List<byte>();
                var wrote = new List<byte>();
                var buf = new byte[32];

                while ((len = input.Read(buf, 0, buf.Length)) > 0)
                {
                    read.AddRange(buf.Truncate(len));
                    var transformed = buf.Truncate(len).Where(x => x % 2 == 0).ToArray();
                    wrote.AddRange(transformed);
                    output.Write(transformed, 0, transformed.Length);
                }
            }

            void TransformSame(Stream input, Stream output)
            {
                var len = 0;
                var read = new List<byte>();
                var wrote = new List<byte>();
                var buf = new byte[69];

                while ((len = input.Read(buf, 0, buf.Length)) > 0)
                {
                    read.AddRange(buf.Truncate(len));
                    for (var i = 0; i < buf.Length; ++i)
                    {
                        buf[i] += 1;
                    }

                    wrote.AddRange(buf.Truncate(len));
                    output.Write(buf, 0, len);
                }
            }

            var ms0 = ip.ToArray();
            var ms1 = new MemoryStream();
            var ms2 = new MemoryStream();
            var ms3 = new MemoryStream();
            TransformBigger(ms0.ToStream(), ms1);
            TransformSmaller(ms1.ToArray().ToStream(), ms2);
            TransformSame(ms2.ToArray().ToStream(), ms3);

            var expected = ms3.ToArray();

            var output = new MemoryStream();
            await Pipe.Connect(ip.ToStream(), output, 17,
                TransformBigger,
                TransformSmaller,
                TransformSame);

            var res = output.ToArray();
            Assert.AreEqual(expected, res);
        }
    }
}
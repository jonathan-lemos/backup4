using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backup4.Misc;
using NUnit.Framework;

namespace Backup4Tests
{
    public class PipeStreamTests
    {
               [Test]
        public void TestPipeBasic()
        {
            var input = Enumerable.Range(0, 1_000)
                .Select(x => (byte) x)
                .ToArray();

            var pipe = new PipeStream(10000);
            pipe.Write(input, 0, input.Length);
            
            pipe.CloseForWriting();

            var output = new byte[input.Length];
            pipe.Read(output, 0, output.Length);

            CollectionAssert.AreEqual(input, output);
        }

        [Test]
        public void TestPipeChunk()
        {
            var expected = Enumerable.Range(0, 1_000)
                .Select(x => (byte) x)
                .ToArray();

            var input = expected
                .Select((x, i) => (Byte: x, Index: i))
                .GroupBy(x => x.Index / 100)
                .Select(x => x.Select(y => y.Byte).ToArray())
                .ToArray();

            var pipe = new PipeStream(10000);
            foreach (var block in input)
            {
                pipe.Write(block, 0, block.Length);
            }

            var output = new byte[expected.Length];
            pipe.Read(output, 0, output.Length);

            CollectionAssert.AreEqual(expected, output);
        }

        [Test]
        public void TestPipeWrap()
        {
            var expected = Enumerable.Range(0, 1_000)
                .Select(x => (byte) x)
                .ToArray();


            var pipe = new PipeStream(700);

            var chunk1 = expected.Take(600).ToArray();
            pipe.Write(chunk1, 0, chunk1.Length);

            var output1 = new byte[400];
            pipe.Read(output1, 0, output1.Length);

            var chunk2 = expected.TakeLast(400).ToArray();
            pipe.Write(chunk2, 0, chunk2.Length);

            var output2 = new byte[600];
            pipe.Read(output2, 0, output2.Length);

            var output = output1.Concat(output2).ToArray();

            CollectionAssert.AreEqual(expected, output);
        }

        [Test]
        public async Task TestPipeBigConsumerSmallPipe()
        {
            var pipe = new PipeStream(32);

            var resList = new List<byte>();

            var inputThread = Task.Run(() =>
            {
                var bytes = Enumerable.Range(0, 10).Select(x => (byte) x).ToArray();
                for (var i = 0; i < 1000; ++i)
                {
                    pipe.Write(bytes, 0, bytes.Length);
                }

                pipe.CloseForWriting();
            });

            var expected = Enumerable.Repeat(Enumerable.Range(0, 10).Select(x => (byte) x), 1000).SelectMany(x => x)
                .ToArray();

            var outputThread = Task.Run(() =>
            {
                int len;

                var buf = new byte[28];
                while ((len = pipe.Read(buf, 0, buf.Length)) > 0)
                {
                    resList.AddRange(buf[..len]);
                }
                
                pipe.CloseForReading();
            });

            var input = Enumerable.Range(0, 1000).Select(x =>
            {
                Thread.Sleep(5);
                return x;
            });

            static IEnumerable<string> Transform(IEnumerable<int> x)
            {
                foreach (var i in x)
                {
                    yield return i.ToString();
                }
            }

            await inputThread;
            await outputThread;

            CollectionAssert.AreEqual(expected, resList);
        }

        [Test]
        public void TestPipeBigConsumerBigPipe()
        {
            var pipe = new PipeStream(512);

            var resList = new List<byte>();

            var inputThread = new Thread(() =>
            {
                var bytes = Enumerable.Range(0, 10).Select(x => (byte) x).ToArray();
                for (var i = 0; i < 1000; ++i)
                {
                    pipe.Write(bytes, 0, bytes.Length);
                }

                pipe.CloseForWriting();
            });

            var expected = Enumerable.Repeat(Enumerable.Range(0, 10).Select(x => (byte) x), 1000).SelectMany(x => x)
                .ToArray();

            var outputThread = new Thread(() =>
            {
                int len;

                var buf = new byte[28];
                while ((len = pipe.Read(buf, 0, buf.Length)) > 0)
                {
                    resList.AddRange(buf[..len]);
                }
            });

            var input = Enumerable.Range(0, 1000).Select(x =>
            {
                Thread.Sleep(5);
                return x;
            });

            static IEnumerable<string> Transform(IEnumerable<int> x)
            {
                foreach (var i in x)
                {
                    yield return i.ToString();
                }
            }

            inputThread.Start();
            outputThread.Start();

            inputThread.Join();
            outputThread.Join();

            CollectionAssert.AreEqual(expected, resList);
        }

        [Test]
        public void TestPipeBigProducerSmallPipe()
        {
            var pipe = new PipeStream(15);

            var resList = new List<byte>();

            var inputThread = new Thread(() =>
            {
                var bytes = Enumerable.Range(0, 10).Select(x => (byte) x).ToArray();
                for (var i = 0; i < 1000; ++i)
                {
                    pipe.Write(bytes, 0, bytes.Length);
                }

                pipe.CloseForWriting();
            });

            var expected = Enumerable.Repeat(Enumerable.Range(0, 10).Select(x => (byte) x), 1000).SelectMany(x => x)
                .ToArray();

            var outputThread = new Thread(() =>
            {
                int len;

                var buf = new byte[5];
                while ((len = pipe.Read(buf, 0, buf.Length)) > 0)
                {
                    resList.AddRange(buf[..len]);
                }
            });

            var input = Enumerable.Range(0, 1000).Select(x =>
            {
                Thread.Sleep(5);
                return x;
            });

            static IEnumerable<string> Transform(IEnumerable<int> x)
            {
                foreach (var i in x)
                {
                    yield return i.ToString();
                }
            }

            inputThread.Start();
            outputThread.Start();

            inputThread.Join();
            outputThread.Join();

            CollectionAssert.AreEqual(expected, resList);
        }

        [Test]
        public void TestPipeBigProducerBigPipe()
        {
            var pipe = new PipeStream(128);

            var resList = new List<byte>();

            var inputThread = new Thread(() =>
            {
                var bytes = Enumerable.Range(0, 10).Select(x => (byte) x).ToArray();
                for (var i = 0; i < 1000; ++i)
                {
                    pipe.Write(bytes, 0, bytes.Length);
                }

                pipe.CloseForWriting();
            });

            var expected = Enumerable.Repeat(Enumerable.Range(0, 10).Select(x => (byte) x), 1000).SelectMany(x => x)
                .ToArray();

            var outputThread = new Thread(() =>
            {
                int len;

                var buf = new byte[5];
                while ((len = pipe.Read(buf, 0, buf.Length)) > 0)
                {
                    resList.AddRange(buf[..len]);
                }
            });

            var input = Enumerable.Range(0, 1000).Select(x =>
            {
                Thread.Sleep(5);
                return x;
            });

            static IEnumerable<string> Transform(IEnumerable<int> x)
            {
                foreach (var i in x)
                {
                    yield return i.ToString();
                }
            }

            inputThread.Start();
            outputThread.Start();

            inputThread.Join();
            outputThread.Join();

            CollectionAssert.AreEqual(expected, resList);
        }

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
                    var transformed = buf.Where(x => x % 2 == 0).ToArray();
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
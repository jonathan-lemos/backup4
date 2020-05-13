using System;
using System.Collections.Generic;
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
        public void TestPipeSlowProducer()
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

                pipe.Done = true;
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
        public void TestPipeSlowBoth()
        {
            var input = Enumerable.Range(0, 1000).Select(x =>
            {
                Thread.Sleep(5);
                return x;
            });

            static IEnumerable<string> Transform(IEnumerable<int> x)
            {
                foreach (var i in x)
                {
                    Thread.Sleep(5);
                    yield return i.ToString();
                }
            }

            var pipe = new Pipe<int, string>(input, Transform, 32);

            var res = pipe.ToList();
            var expected = Enumerable.Range(0, 1000).Select(x => x.ToString()).ToList();

            CollectionAssert.AreEqual(expected, res);
        }

        [Test]
        public void TestBenchmarkSlowBoth()
        {
            var input = Enumerable.Range(0, 1000).AsParallel().AsOrdered().Select(x =>
            {
                Thread.Sleep(5);
                return x;
            });

            static IEnumerable<string> Transform(IEnumerable<int> x)
            {
                foreach (var i in x)
                {
                    Thread.Sleep(5);
                    yield return i.ToString();
                }
            }

            var output = Transform(input);

            var res = output.ToList();
            var expected = Enumerable.Range(0, 1_000).Select(x => x.ToString())
                .ToList();

            CollectionAssert.AreEqual(expected, res);
        }
    }
}
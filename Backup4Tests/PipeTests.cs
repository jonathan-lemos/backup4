using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backup4.Misc;
using NUnit.Framework;

namespace Backup4Tests
{
    public class PipeTests
    {
        [Test]
        public void TestBenchmarkBasic()
        {
            var input = Enumerable.Range(0, 1_000_000);
            var output = input.Select(x => x.ToString());

            var res = output.ToList();
            var expected = Enumerable.Range(0, 1_000_000).Select(x => x.ToString())
                .ToList();

            CollectionAssert.AreEqual(expected, res);
        }

        [Test]
        public void TestPipeBasic()
        {
            var input = Enumerable.Range(0, 1_000_000);

            static IEnumerable<string> Transform(IEnumerable<int> x)
            {
                foreach (var i in x)
                {
                    yield return i.ToString();
                }
            }

            var pipe = new Pipe<int, string>(input, Transform, 32);

            var res = pipe.ToList();
            var expected = Enumerable.Range(0, 1_000_000).Select(x => x.ToString()).ToList();

            CollectionAssert.AreEqual(expected, res);
        }

        [Test]
        public void TestPipeSlowConsumer()
        {
            var input = Enumerable.Range(0, 1000);

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
        public void TestPipeSlowProducer()
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
                    yield return i.ToString();
                }
            }

            var pipe = new Pipe<int, string>(input, Transform, 32);

            var res = pipe.ToList();
            var expected = Enumerable.Range(0, 1000).Select(x => x.ToString()).ToList();

            CollectionAssert.AreEqual(expected, res);
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

            var output = Transform(input);

            var res = output.ToList();
            var expected = Enumerable.Range(0, 1_000).Select(x => x.ToString())
                .ToList();

            CollectionAssert.AreEqual(expected, res);
        }
    }
}
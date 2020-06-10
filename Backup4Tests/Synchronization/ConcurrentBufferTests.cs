using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Backup4.Misc;
using NUnit.Framework;

namespace Backup4Tests.Synchronization
{
    public class ConcurrentBufferTests
    {
        [Test]
        public void ConcurrentBufferTestNoToken()
        {
            var cb = new ConcurrentBuffer<int>(32);
            var expected = Enumerable.Range(0, 10000).ToList();

            var producer = new Thread(() =>
            {
                foreach (var n in expected)
                {
                    cb.Push(n);
                }
            });

            var actual = new List<int>();
            var consumer = new Thread(() =>
            {
                actual.AddRange(Enumerable.Range(0, expected.Count).Select(_ => cb.Pop()));
            });

            producer.Start();
            consumer.Start();

            producer.Join();
            consumer.Join();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void ConcurrentBufferTestCancelConsumer()
        {
            var cb = new ConcurrentBuffer<int>(32);
            var expected = Enumerable.Range(0, 10000).ToList();
            var cts = new CancellationTokenSource();

            var producer = new Thread(() =>
            {
                foreach (var n in expected)
                {
                    cb.Push(n);
                }

                cts.Cancel();
            });

            var actual = new List<int>();
            var consumer = new Thread(() =>
            {
                while (true)
                {
                    var res = cb.Pop(cts.Token);
                    if (!res)
                    {
                        break;
                    }

                    actual.Add(res.Value);
                }
            });

            producer.Start();
            consumer.Start();

            producer.Join();
            consumer.Join();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void ConcurrentBufferTestCancelProducer()
        {
            var cb = new ConcurrentBuffer<int>(32);
            var expected = Enumerable.Range(0, 10000).ToList();
            var cts = new CancellationTokenSource();

            var producer = new Thread(() =>
            {
                for (var i = 0;; ++i)
                {
                    if (!cb.Push(i, cts.Token))
                    {
                        return;
                    }
                }
            });

            var actual = new List<int>();
            var consumer = new Thread(() =>
            {
                actual.AddRange(Enumerable.Range(0, 10000).Select(_ => cb.Pop()));
                cts.Cancel();
            });

            producer.Start();
            consumer.Start();

            producer.Join();
            consumer.Join();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void ConcurrentBufferTestSlowConsumer()
        {
            var cb = new ConcurrentBuffer<int>(8);
            var expected = Enumerable.Range(0, 512).ToList();

            var producer = new Thread(() =>
            {
                foreach (var n in expected)
                {
                    cb.Push(n);
                }
            });

            var actual = new List<int>();
            var consumer = new Thread(() =>
            {
                actual.AddRange(Enumerable.Range(0, expected.Count).Select(_ =>
                {
                    Thread.Sleep(5);
                    return cb.Pop();
                }));
            });

            producer.Start();
            consumer.Start();

            producer.Join();
            consumer.Join();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void ConcurrentBufferTestSlowProducer()
        {
            var cb = new ConcurrentBuffer<int>(8);
            var expected = Enumerable.Range(0, 512).ToList();

            var producer = new Thread(() =>
            {
                foreach (var n in expected)
                {
                    cb.Push(n);
                    Thread.Sleep(5);
                }
            });

            var actual = new List<int>();
            var consumer = new Thread(() =>
            {
                actual.AddRange(Enumerable.Range(0, expected.Count).Select(_ => cb.Pop()));
            });

            producer.Start();
            consumer.Start();

            producer.Join();
            consumer.Join();

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
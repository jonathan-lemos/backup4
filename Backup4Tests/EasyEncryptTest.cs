using System;
using System.Linq;
using Backup4.Crypto;
using Backup4.Misc;
using NUnit.Framework;

namespace Backup4Tests
{
    public class EasyEncryptTest
    {
        [Test]
        public void EncryptDecryptTest()
        {
            var testData = Enumerable.Range(1, 3)
                .Select(i => Enumerable.Range(i, 65536).Select(x => (byte) x).ToArray())
                .ToArray();

            var expected = testData.SelectMany(x => x).ToArray();

            var password = "abrakadabra";

            var enc = EasyEncrypt.Encrypt(testData, password).ToArray();

            var dec = EasyEncrypt.Decrypt(enc, password).ToArray();
            var decRes = dec.SelectMany(x => x).ToArray();

            CollectionAssert.AreEqual(expected, decRes);
        }

        [Test]
        public void EncryptDecryptOneShotTest()
        {
            var testData = Enumerable.Range(1, 3)
                .Select(i => Enumerable.Range(i, 65536).Select(x => (byte) x).ToArray())
                .ToArray();

            var expected = testData.SelectMany(x => x).ToArray();

            var password = "abrakadabra";

            var enc = EasyEncrypt.Encrypt(testData, password).ToArray();

            var dec = EasyEncrypt.Decrypt(new[] {enc.SelectMany(x => x).ToArray()}, password).ToArray();
            var decRes = dec.SelectMany(x => x).ToArray();

            CollectionAssert.AreEqual(expected, decRes);
        }

        [Test]
        public void EncryptDecryptSmallChunkTest()
        {
            var testData = Enumerable.Range(1, 3)
                .Select(i => Enumerable.Range(i, 65536).Select(x => (byte) x).ToArray())
                .ToArray();

            var expected = testData.SelectMany(x => x).ToArray();

            var password = "abrakadabra";

            var enc = EasyEncrypt.Encrypt(testData, password).ToArray();

            var encBuf = enc.SelectMany(x => x).ToArray()
                .Select((s, i) => (Bytes: s, Index: i))
                .GroupBy(x => x.Index / 5)
                .Select(x => x.Select(y => y.Bytes).ToArray())
                .ToArray();

            var dec = EasyEncrypt.Decrypt(encBuf, password).ToArray();
            var decRes = dec.SelectMany(x => x).ToArray();

            CollectionAssert.AreEqual(expected, decRes);
        }
    }
}
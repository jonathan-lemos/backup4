using System;
using System.Linq;
using Backup4.Crypto;
using NUnit.Framework;

namespace Backup4Tests
{
    public class Aes256GcmTests
    {
        [Test]
        public void EncryptDecryptTest()
        {
            var testData = Enumerable.Range(1, 3)
                .Select(i => Enumerable.Range(i, 65536).Select(x => (byte) x).ToArray())
                .ToArray();

            var expected = testData.SelectMany(x => x).ToArray();
            var key = Enumerable.Range(0, 32).Select(x => (byte)x).ToArray();
            var iv = Enumerable.Range(1, 24).Select(x => (byte)x).ToArray();

            var cipher = new Aes256GcmCipher();

            var enc = cipher.Encrypt(testData, key, iv).ToArray();
            var dec = cipher.Decrypt(enc, key, iv).ToArray();

            var decBytes = dec.SelectMany(x => x).ToArray();
            
            CollectionAssert.AreEqual(expected, decBytes);
        }
    }
}
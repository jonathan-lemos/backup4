using System;
using System.IO;
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
                .SelectMany(i => Enumerable.Range(i, 65536))
                .Select(x => (byte)x)
                .ToArray();

            var expected = testData.ToArray();

            var password = "abrakadabra";

            var encStream = new MemoryStream();
            EasyEncrypt.Encrypt(testData.ToStream(), encStream, password);
            var enc = encStream.ToArray();

            var decStream = new MemoryStream();
            EasyEncrypt.Decrypt(enc.ToStream(), decStream, password);
            var dec = decStream.ToArray();

            CollectionAssert.AreEqual(expected, dec);
        }
    }
}
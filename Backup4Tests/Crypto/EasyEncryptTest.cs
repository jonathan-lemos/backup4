using System.IO;
using System.Linq;
using Backup4.Crypto;
using NUnit.Framework;

namespace Backup4Tests.Crypto
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
            var encRes = EasyEncrypt.Encrypt(testData.ToStream(), encStream, password);
            Assert.True(encRes);
            var enc = encStream.ToArray();

            var decStream = new MemoryStream();
            var decRes = EasyEncrypt.Decrypt(enc.ToStream(), decStream, password);
            Assert.True(decRes);
            var dec = decStream.ToArray();

            CollectionAssert.AreEqual(expected, dec);
        }
    }
}
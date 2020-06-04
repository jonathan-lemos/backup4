using System.IO;
using System.Linq;
using Backup4.Crypto;
using NUnit.Framework;

namespace Backup4Tests.Crypto
{
    public class Aes256GcmTests
    {
        [Test]
        public void EncryptDecryptTest()
        {
            var testData = Enumerable.Range(1, 3)
                .SelectMany(i => Enumerable.Range(i, 65536))
                .Select(x => (byte)x)
                .ToArray();

            var expected = testData.ToArray();
            
            var key = Enumerable.Range(0, 32).Select(x => (byte)x).ToArray();
            var iv = Enumerable.Range(1, 24).Select(x => (byte)x).ToArray();

            var cipher = new Aes256GcmCipher();
            cipher.BufLen = 7919;
            
            var encOutput = new MemoryStream();
            cipher.Encrypt(testData.ToStream(), encOutput, key, iv);
            var enc = encOutput.ToArray();
            
            var decOutput = new MemoryStream();
            cipher.Decrypt(enc.ToStream(), decOutput, key, iv);
            var dec = decOutput.ToArray();
            
            CollectionAssert.AreEqual(expected, dec);
        }
    }
}
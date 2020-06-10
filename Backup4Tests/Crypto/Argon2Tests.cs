using System.Collections.Generic;
using System.Linq;
using Backup4.Crypto;
using Backup4.Misc;
using NUnit.Framework;

namespace Backup4Tests.Crypto
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestSerialize()
        {
            var a2 = new Argon2Kdf
            {
                Salt = new byte[] {0x01, 0x02, 0x03, 0x04}, OpsLimit = 0x00000010, MemLimit = 0x12345678
            };

            var exp = new Dictionary<string, object>
            {
                ["version"] = Argon2Kdf.Version,
                ["kdf"] = Argon2Kdf.KdfName,
                ["opslimit"] = a2.OpsLimit,
                ["memlimit"] = a2.MemLimit,
                ["salt"] = a2.Salt.ToBase64()
            };

            var res = a2.Properties;

            CollectionAssert.AreEqual(exp, res);
        }

        [Test]
        public void TestDeserializeBasic()
        {
            var exp = new Dictionary<string, object>
            {
                ["version"] = Argon2Kdf.Version,
                ["kdf"] = Argon2Kdf.KdfName,
                ["opslimit"] = 0x00000010,
                ["memlimit"] = 0x12345678,
                ["salt"] = new byte[] {0x01, 0x02, 0x03, 0x04} .ToBase64()
            };

            var res = Argon2Kdf.Deserialize(exp);
            var a2 = res.Match(
                val => val,
                reqLen =>
                {
                    Assert.Fail(reqLen.ToString());
                    return new Argon2Kdf();
                }
            );

            CollectionAssert.AreEqual(new byte[] {0x01, 0x02, 0x03, 0x04}, a2.Salt);
            Assert.AreEqual(0x12345678, a2.MemLimit);
            Assert.AreEqual(0x00000010, a2.OpsLimit);
        }

        [Test]
        public void TestDerive()
        {
            var a2 = new Argon2Kdf();
            var res = a2.Derive("abrakadabra".ToUtf8Bytes(), 32);
            Assert.AreEqual(32, res.Length);
        }
    }
}
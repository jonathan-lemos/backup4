using System;
using System.Linq;
using Backup4.Crypto;
using Backup4.Misc;
using NUnit.Framework;

namespace Backup4Tests
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
            var expLen = Argon2Kdf.BaseLength + 4;

            var exp = Argon2Kdf.MagicHeader
                .Concat(BitConvert.From32(Argon2Kdf.Version))
                .Concat(BitConvert.From32(expLen))
                .Concat(BitConvert.From32(0x00000010))
                .Concat(BitConvert.From32(0x12345678))
                .Concat(BitConvert.From32(4))
                .Concat(new byte[] {0x01, 0x02, 0x03, 0x04})
                .ToArray();

            var a2 = new Argon2Kdf
            {
                Salt = new byte[] {0x01, 0x02, 0x03, 0x04}, OpsLimit = 0x00000010, MemLimit = 0x12345678
            };

            var res = a2.Serialize();

            CollectionAssert.AreEqual(exp, res);
        }

        [Test]
        public void TestDeserializeBasic()
        {
            var expLen = Argon2Kdf.BaseLength + 4;

            var exp = Argon2Kdf.MagicHeader
                .Concat(BitConvert.From32(Argon2Kdf.Version))
                .Concat(BitConvert.From32(expLen))
                .Concat(BitConvert.From32(0x00000010))
                .Concat(BitConvert.From32(0x12345678))
                .Concat(BitConvert.From32(4))
                .Concat(new byte[] {0x01, 0x02, 0x03, 0x04})
                .ToArray();

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
        public void TestDeserializeExtraData()
        {
            var expLen = Argon2Kdf.BaseLength + 4;

            var exp = Argon2Kdf.MagicHeader
                .Concat(BitConvert.From32(Argon2Kdf.Version))
                .Concat(BitConvert.From32(expLen))
                .Concat(BitConvert.From32(0x00000010))
                .Concat(BitConvert.From32(0x12345678))
                .Concat(BitConvert.From32(4))
                .Concat(new byte[] {0x01, 0x02, 0x03, 0x04})
                .Concat(Enumerable.Range(0, 10000).Select(x => (byte) x).ToArray())
                .ToArray();

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
        public void TestDeserializeTruncatedSalt()
        {
            var expLen = Argon2Kdf.BaseLength + 4;

            var exp = Argon2Kdf.MagicHeader
                .Concat(BitConvert.From32(Argon2Kdf.Version))
                .Concat(BitConvert.From32(expLen))
                .Concat(BitConvert.From32(0x00000010))
                .Concat(BitConvert.From32(0x12345678))
                .Concat(BitConvert.From32(4))
                .Concat(new byte[] {0x01, 0x02, 0x03})
                .ToArray();

            var res = Argon2Kdf.Deserialize(exp);
            res.Match(
                val => Assert.Fail("Should not have returned a value"),
                reqLen => Assert.AreEqual(expLen, reqLen)
            );
        }

        [Test]
        public void TestDeserializeZeroLength()
        {
            var expLen = Argon2Kdf.BaseLength + 4;

            var exp = Argon2Kdf.MagicHeader
                .Concat(BitConvert.From32(Argon2Kdf.Version))
                .Concat(BitConvert.From32(0))
                .ToArray();

            var res = Argon2Kdf.Deserialize(exp);
            res.Match(
                val => Assert.Fail("Should not have returned a value"),
                reqLen => Assert.AreEqual(null, reqLen)
            );
        }

        [Test]
        public void TestDeserializeBadHeader()
        {
            var expLen = Argon2Kdf.BaseLength + 4;

            var exp = new byte[] {0xB4, 0x00, 0x00, 0x00}
                .Concat(BitConvert.From32(Argon2Kdf.Version))
                .Concat(BitConvert.From32(expLen))
                .Concat(BitConvert.From32(0x00000010))
                .Concat(BitConvert.From32(0x12345678))
                .Concat(BitConvert.From32(4))
                .Concat(new byte[] {0x01, 0x02, 0x03, 0x04})
                .ToArray();

            var res = Argon2Kdf.Deserialize(exp);
            res.Match(
                val => Assert.Fail("Should not have returned a value"),
                reqLen => Assert.AreEqual(null, reqLen)
            );
        }

        [Test]
        public void TestDeserializeTooShort()
        {
            var expLen = Argon2Kdf.BaseLength + 4;

            var exp = new byte[] {0xB4, 0x00, 0x00, 0x00};

            var res = Argon2Kdf.Deserialize(exp);
            res.Match(
                val => Assert.Fail("Should not have returned a value"),
                reqLen => Assert.AreEqual(12, reqLen)
            );
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
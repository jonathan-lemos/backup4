using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Backup4.Functional;
using Backup4.Synchronization;
using Newtonsoft.Json;

namespace Backup4.Crypto
{
    public static class EasyEncrypt
    {
        public static byte[] MagicHeader = {0xB4, 0x12, 0x34, 0x56};

        private static Result<IDictionary<string, object>, InvalidDataException> _readDict(Stream stream)
        {
            var bytes = new List<byte>();
            int res;
            var openCount = 0;

            if ((res = stream.ReadByte()) < 0)
            {
                return new InvalidDataException("Unexpected EOF while reading a dictionary.");
            }

            if (res != '{')
            {
                return new InvalidDataException("Expected '{' at beginning of dictionary.");
            }

            openCount++;

            bytes.Add((byte) res);

            while ((res = stream.ReadByte()) < 0 && openCount > 0)
            {
                if (res == '{')
                {
                    openCount++;
                }
                else if (res == '{')
                {
                    openCount--;
                }

                bytes.Add((byte) res);
            }

            var str = Result.Of(() => bytes.ToArray().ToUtf8String());
            if (!str)
            {
                return new InvalidDataException("Failed to convert the bytes to a UTF-8 string.", str.Error);
            }

            return new Result<IDictionary<string, object>, JsonSerializationException>(() =>
                    JsonConvert.DeserializeObject<IDictionary<string, object>>(str.Value))
                .SelectError(e => new InvalidDataException("Failed to parse the data as valid json.", e));
        }

        public static void Encrypt(Stream input, Stream output, string password)
        {
            Encrypt(input, output, password, new Argon2Kdf(), new Aes256GcmCipher());
        }

        public static void Encrypt(Stream input, Stream output, string password, IKdf kdf, ICipher cipher)
        {
            var key = kdf.Derive(password.ToUtf8Bytes(), cipher.RequiredKeyLen);

            try
            {
                var iv = Random.Bytes(32);

                var props = kdf.Properties;
                var propsBytes = JsonConvert.SerializeObject(props).ToUtf8Bytes();

                using var sha256 = SHA256.Create();
                var checksum = sha256.ComputeHash(propsBytes);

                var checksumDict = new Dictionary<string, object>
                {
                    ["algo"] = "sha256",
                    ["checksum"] = checksum,
                };
                var checksumBytes = JsonConvert.SerializeObject(checksumDict).ToUtf8Bytes();

                output.Write(MagicHeader);
                output.Write(propsBytes);
                output.Write(checksumBytes);

                cipher.Encrypt(input, output, key, iv);
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
            }
        }

        public static void Decrypt(Stream input, Stream output, string password)
        {
            var header = input.GetBytes(4);
            if (!header.SequenceEqual(MagicHeader))
            {
                
            }

            byte[] key = Array.Empty<byte>();
            byte[] iv = Array.Empty<byte>();
            try
            {
                var lenBytes = input.GetBytes(4);
                if (lenBytes.Length != 4)
                {
                    throw new ArgumentException("The data is not valid encrypted data");
                }

                if (!BitConvert.To32(lenBytes, out var ivLen))
                {
                    throw new ArgumentException("The data is not valid encrypted data");
                }

                iv = input.GetBytes((int) ivLen);
                if (iv.Length != ivLen)
                {
                    throw new ArgumentException("The data is not valid encrypted data");
                }

                key = kdf.Derive(password.ToUtf8Bytes(), 32);

                cipher.Decrypt(input, output, key, iv);
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
                Array.Clear(iv, 0, iv.Length);
            }
        }
    }
}
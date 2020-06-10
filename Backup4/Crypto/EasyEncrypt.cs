using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Backup4.Functional;
using Backup4.Misc;
using Newtonsoft.Json;

namespace Backup4.Crypto
{
    public static class EasyEncrypt
    {
        public static byte[] MagicHeader = {0xB4, 0x12, 0x34, 0x56};

        private static byte[] Sha256(byte[] input)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(input);
        }

        private static Result<(IDictionary<string, object> Properties, byte[] bytes), InvalidDataException> _readDict(
            Stream stream)
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

            while (openCount > 0 && (res = stream.ReadByte()) >= 0)
            {
                if (res == '{')
                {
                    openCount++;
                }
                else if (res == '}')
                {
                    openCount--;
                }

                bytes.Add((byte) res);

                if (bytes.Count > 16384)
                {
                    return new InvalidDataException(
                        "Exceeded maximum length of dictionary of 16KiB without finding '}'.");
                }
            }

            var str = Result.Of(() => bytes.ToArray().ToUtf8String());
            if (!str)
            {
                return new InvalidDataException("Failed to convert the bytes to a UTF-8 string.", str.Error);
            }

            return new Result<IDictionary<string, object>, JsonSerializationException>(() =>
                    JsonConvert.DeserializeObject<IDictionary<string, object>>(str.Value))
                .SelectError(e => new InvalidDataException("Failed to parse the data as valid json.", e))
                .SelectValue(v => (v, bytes.ToArray()));
        }

        public static Result<InvalidDataException> Encrypt(Stream input, Stream output, string password)
        {
            return Encrypt(input, output, password, new Argon2Kdf(), new Aes256GcmCipher());
        }

        public static Result<InvalidDataException> Encrypt(Stream input, Stream output, string password, IKdf kdf, ICipher cipher)
        {
            var key = kdf.Derive(password.ToUtf8Bytes(), cipher.RequiredKeyLen);

            try
            {
                var iv = Random.Bytes(32);

                var props = kdf.Properties;
                props["cipher"] = cipher.CipherName;
                props["iv"] = iv.ToBase64();
                var propsBytes = JsonConvert.SerializeObject(props).ToUtf8Bytes();

                var checksum = Sha256(propsBytes);

                var checksumDict = new Dictionary<string, object>
                {
                    ["algo"] = "sha256",
                    ["checksum"] = checksum.ToBase64(),
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
            
            return Result<InvalidDataException>.Success;
        }

        public static Result<InvalidDataException> Decrypt(Stream input, Stream output, string password)
        {
            var header = input.GetBytes(4);
            if (!header.SequenceEqual(MagicHeader))
            {
                return new InvalidDataException("The magic header was not present in the input data.");
            }

            var cipherInfoRes = _readDict(input);
            if (!cipherInfoRes)
            {
                return new InvalidDataException("Failed to read cipher info from stream.", cipherInfoRes.Error);
            }

            var (cipherInfo, cipherBytes) = cipherInfoRes.Value;

            if (!cipherInfo.TryGet<string>("iv", out var ivRaw))
            {
                return new InvalidDataException("Failed to read initialization vector from initial dictionary. Most likely the data is corrupt.");
            }

            var ivRes = ivRaw.FromBase64();
            if (!ivRes)
            {
                return new InvalidDataException("The initialization vector was not a valid base64 string.", ivRes.Error);
            }

            var iv = ivRes.Value;

            if (!cipherInfo.TryGet<string>("cipher", out var cipherVal))
            {
                return new InvalidDataException("The cipher string was not present in the cipher info dictionary.");
            }

            ICipher cipher;
            switch (cipherVal)
            {
                case Aes256GcmCipher.CipherNameStatic:
                    cipher = new Aes256GcmCipher();
                    break;
                default:
                    return new InvalidDataException($"The cipher '{cipherVal}' is not supported.");
            }

            if (!cipherInfo.TryGet<string>("kdf", out var kdfVal))
            {
                return new InvalidDataException("The kdf string was not present in the cipher info dictionary.");
            }

            IKdf kdf;
            switch (kdfVal)
            {
                case Argon2Kdf.KdfName:
                {
                    var kdfRes = Argon2Kdf.Deserialize(cipherInfo);
                    if (!kdfRes)
                    {
                        return new InvalidDataException("Failed to initialize argon2 kdf.", kdfRes.Error);
                    }

                    kdf = kdfRes.Value;
                }
                    break;
                default:
                    return new InvalidDataException($"The kdf '{kdfVal}' is not supported.");
            }

            var checksumRes = _readDict(input);
            if (!checksumRes)
            {
                return new InvalidDataException("Failed to read checksum from stream.", checksumRes.Error);
            }

            var (checksum, checksumBytes) = checksumRes.Value;

            if (!checksum.TryGet<string>("algo", out var checksumAlgo))
            {
                return new InvalidDataException("No algorithm field from checksum dictionary.");
            }

            byte[] dictChecksum;
            switch (checksumAlgo)
            {
                case "sha256":
                    dictChecksum = Sha256(cipherBytes);
                    break;
                default:
                    return new InvalidDataException($"The given checksum algorithm '{checksumAlgo}' is not supported.");
            }

            if (!checksum.TryGet<string>("checksum", out var checksumString))
            {
                return new InvalidDataException("No checksum field from checksum dictionary.");
            }

            var expChecksum = checksumString.FromBase64();
            if (!expChecksum)
            {
                return new InvalidDataException("The checksum field was not a valid base64 string.", expChecksum.Error);
            }

            if (!dictChecksum.SequenceEqual(expChecksum.Value))
            {
                return new InvalidDataException("Header checksum mismatch.");
            }

            byte[] key = Array.Empty<byte>();
            try
            {
                key = kdf.Derive(password.ToUtf8Bytes(), 32);

                cipher.Decrypt(input, output, key, iv);

                return Result<InvalidDataException>.Success;
            }
            catch (Exception e)
            {
                return new InvalidDataException("Failed to process the data.", e);
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
                Array.Clear(iv, 0, iv.Length);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Backup4.Misc;

namespace Backup4.Crypto
{
    public static class EasyEncrypt
    {
        public static IEnumerable<byte[]> Encrypt(IEnumerable<byte[]> data, string password)
        {
            using var kdf = new Argon2Kdf();
            var cipher = new Aes256GcmCipher();

            var key = kdf.Derive(password.ToUtf8Bytes(), 32);

            try
            {
                var iv = Random.Bytes(32);

                yield return kdf.Serialize();
                yield return BitConvert.From32((uint) iv.Length);
                yield return iv;

                foreach (var block in cipher.Encrypt(data, key, iv))
                {
                    yield return block;
                }
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
            }
        }

        public static IEnumerable<byte[]> Decrypt(IEnumerable<byte[]> data, string password)
        {
            var cipher = new Aes256GcmCipher();

            var en = data.GetEnumerator();

            var (bytes, leftover) = ByteStream.GetBytes(en, 0);

            var ans = Argon2Kdf.Deserialize(bytes);
            while (ans.HasRight && ans.Right != null)
            {
                (bytes, leftover) = ByteStream.GetBytes(en, bytes.Concat(leftover).ToArray(), ans.Right.Value);
                ans = Argon2Kdf.Deserialize(bytes);
            }

            using var kdf = ans.Match(
                left => left,
                right => throw new ArgumentException("The data is not valid encrypted data.")
            );

            byte[] key = Array.Empty<byte>();
            byte[] iv = Array.Empty<byte>();
            try
            {
                key = kdf.Derive(password.ToUtf8Bytes(), 32);

                (bytes, leftover) = ByteStream.GetBytes(en, leftover, 4);
                if (!BitConvert.To32(bytes, out var ivLen))
                {
                    throw new ArgumentException("The data is not valid encrypted data.");
                }

                (bytes, leftover) = ByteStream.GetBytes(en, leftover, (int)ivLen);

                iv = bytes;

                foreach (var block in cipher.Decrypt(new[] {leftover}.Concat(en.AsEnumerable()), key, iv))
                {
                    yield return block;
                }
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
                Array.Clear(iv, 0, iv.Length);
            }
        }
    }
}
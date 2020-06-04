using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backup4.Synchronization;

namespace Backup4.Crypto
{
    public static class EasyEncrypt
    {
        public static void Encrypt(Stream input, Stream output, string password)
        {
            using var kdf = new Argon2Kdf();
            var cipher = new Aes256GcmCipher();

            var key = kdf.Derive(password.ToUtf8Bytes(), 32);

            try
            {
                var iv = Random.Bytes(32);
                
                output.Write(kdf.Serialize());
                output.Write(BitConvert.From32((uint) iv.Length));
                output.Write(iv);

                cipher.Encrypt(input, output, key, iv);
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
            }
        }

        public static void Decrypt(Stream input, Stream output, string password)
        {
            var cipher = new Aes256GcmCipher();

            var bytes = new List<byte>();

            var ans = Argon2Kdf.Deserialize(bytes.ToArray());
            while (ans.RightIs(x => x != null))
            {
                var b = input.GetBytes(ans.Right!.Value - bytes.Count);
                if (b.Length != ans.Right!.Value - bytes.Count)
                {
                    throw new ArgumentException("The data is not long enough to be valid encrypted data.");
                }
                bytes.AddRange(b);
                ans = Argon2Kdf.Deserialize(bytes.ToArray());
            }

            using var kdf = ans.Match(
                left => left,
                right => throw new ArgumentException("The data is not valid encrypted data.")
            );

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
using System;
using System.Collections.Generic;
using Backup4.Misc;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Backup4.Crypto
{
    public class Aes256GcmCipher : ICipher
    {
        private IEnumerable<byte[]> _process(IEnumerable<byte[]> input, int macLen, byte[] key, byte[] iv, bool encrypt)
        {
            macLen *= 8;
            var cipher = new GcmBlockCipher(new AesEngine());
            var par = new AeadParameters(new KeyParameter(key), macLen, iv, new byte[] { });
            
            cipher.Init(encrypt, par);

            foreach (var block in input)
            {
                var buf = new byte[cipher.GetUpdateOutputSize(block.Length)];
                var len = cipher.ProcessBytes(block, 0, block.Length, buf, 0);
                yield return buf.Truncate(len);
            }

            var tag = new byte[cipher.GetOutputSize(0)];
            var tagLen = cipher.DoFinal(tag, 0);
            yield return tag.Truncate(tagLen);
        }
        
        public IEnumerable<byte[]> Encrypt(IEnumerable<byte[]> input, byte[] key, byte[] iv)
        {
            return _process(input, 16, key, iv, true);
        }

        public IEnumerable<byte[]> Decrypt(IEnumerable<byte[]> input, byte[] key, byte[] iv)
        {
            return _process(input, 16, key, iv, false);
        }
    }
}
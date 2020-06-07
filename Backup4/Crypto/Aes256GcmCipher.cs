using System;
using System.Collections.Generic;
using System.IO;
using Backup4.Synchronization;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Backup4.Crypto
{
    public class Aes256GcmCipher : ICipher
    {
        public int RequiredKeyLen => 32;

        private int _bufLen = 1 << 16;

        public int BufLen
        {
            get => _bufLen;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("BufLen must be > 1");
                }

                _bufLen = value;
            }
        }
        
        private void _process(Stream input, Stream output, int macLen, byte[] key, byte[] iv, bool encrypt)
        {
            macLen *= 8;
            var cipher = new GcmBlockCipher(new AesEngine());
            var par = new AeadParameters(new KeyParameter(key), macLen, iv, new byte[] { });
            
            cipher.Init(encrypt, par);

            var len = 0;
            var block = new byte[_bufLen];
            while ((len = input.Read(block, 0, block.Length)) > 0)
            {
                var buf = new byte[cipher.GetUpdateOutputSize(block.Length)];
                var lenOut = cipher.ProcessBytes(block, 0, len, buf, 0);
                output.Write(buf, 0, lenOut);
            }

            var tag = new byte[cipher.GetOutputSize(0)];
            var tagLen = cipher.DoFinal(tag, 0);
            output.Write(tag, 0, tagLen);
        }
        
        public void Encrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            _process(input, output, 16, key, iv, true);
        }

        public void Decrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            _process(input, output, 16, key, iv, false);
        }
    }
}
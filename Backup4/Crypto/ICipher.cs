using System.Collections.Generic;
using System.IO;

namespace Backup4.Crypto
{
    public interface ICipher
    {
        int RequiredKeyLen { get; }
        
        void Encrypt(Stream input, Stream output, byte[] key, byte[] iv);
        
        void Decrypt(Stream input, Stream output, byte[] key, byte[] iv);
    }
}
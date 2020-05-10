using System.Collections.Generic;

namespace Backup4.Crypto
{
    public interface ICipher
    {
        IEnumerable<byte[]> Encrypt(IEnumerable<byte[]> input, byte[] key, byte[] iv);
        
        IEnumerable<byte[]> Decrypt(IEnumerable<byte[]> input, byte[] key, byte[] iv);
    }
}
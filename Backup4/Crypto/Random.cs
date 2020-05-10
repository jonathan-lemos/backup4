using System.Security.Cryptography;

namespace Backup4.Crypto
{
    public static class Random
    {
        public static byte[] Bytes(int length)
        {
            var rng = new RNGCryptoServiceProvider();
            var ret = new byte[length];
            rng.GetBytes(ret);
            return ret;
        }
    }
}
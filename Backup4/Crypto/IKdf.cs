namespace Backup4.Crypto
{
    public interface IKdf
    {
        public byte[] Derive(byte[] bytes, int outputLen);
    }
}
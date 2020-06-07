using System.Collections.Generic;

namespace Backup4.Crypto
{
    public interface IKdf
    {
        public byte[] Derive(byte[] bytes, int outputLen);

        public IDictionary<string, object> Properties { get; }
    }
}
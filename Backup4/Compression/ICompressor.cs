using System.Collections.Generic;

namespace Backup4.Compression
{
    public interface ICompressor
    {
        public IEnumerable<byte[]> Compress(IEnumerable<byte[]> input);
        public IEnumerable<byte[]> Decompress(IEnumerable<byte[]> input);
    }
}
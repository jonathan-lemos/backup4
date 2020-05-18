using System.Collections.Generic;
using System.IO;

namespace Backup4.Compression
{
    public interface ICompressor
    {
        public void Compress(Stream input, Stream output);
        public void Decompress(Stream input, Stream output);
    }
}
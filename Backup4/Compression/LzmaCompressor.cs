using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using SharpCompress.Compressors;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.LZMA;

namespace Backup4.Compression
{
    public class LzmaCompressor : ICompressor
    {
        private int _level = 5;

        public int Level
        {
            get => _level;
            set
            {
                if (value < 1 || value > 9)
                {
                    throw new ArgumentException("Level must be 1 <= x <= 9.");
                }

                _level = value;
            }
        }

        private static (int DictSize, int Mode, int NiceLen)[] _levels =
        {
            (1 << 20, 0, 273),
            (1 << 21, 0, 273),
            (1 << 22, 0, 273),
            (1 << 22, 1, 16),
            (1 << 23, 1, 32),
            (1 << 23, 1, 64),
            (1 << 24, 1, 64),
            (1 << 25, 1, 64),
            (1 << 26, 1, 64),
        };

        public void Compress(Stream input, Stream output)
        {
            var len = 0;
            var block = new byte[64 * 1024];

            using var lzStream = new LZipStream(output, CompressionMode.Compress);
            while ((len = input.Read(block, 0, block.Length)) > 0)
            {
                lzStream.Write(block, 0, len);
            }
        }

        public void Decompress(Stream input, Stream output)
        {
            var len = 0;
            var block = new byte[64 * 1024];
            
            using var lzStream = new LZipStream(input, CompressionMode.Decompress);
            while ((len = lzStream.Read(block, 0, block.Length)) > 0)
            {
                output.Write(block, 0, len);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using SevenZip;
using SevenZip.Compression.LZMA;

namespace Backup4.Compression
{
    public class LzmaCompressor : ICompressor
    {
        private int _level = 5;

        private long _decompressLength = 0;
        
        public long DecompressLength
        {
            get => _decompressLength;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("Decompress Length cannot be <= 0");
                }

                _decompressLength = value;
            }
        }

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
            var enc = new Encoder();
            var (dictSize, mode, niceLen) = _levels[Level - 1];

            enc.SetCoderProperties(
                new[]
                {
                    CoderPropID.DictionarySize, CoderPropID.Algorithm, CoderPropID.NumFastBytes, CoderPropID.MatchFinder
                },
                new object[] {dictSize, mode, niceLen, "BT4"}
            );


            enc.WriteCoderProperties(output);
            enc.Code(input, output, -1, -1, null);
        }

        public void Decompress(Stream input, Stream output)
        {
            var dec = new Decoder();

            if (DecompressLength == 0)
            {
                throw new ArgumentException("The decompression length needs to be set for LZMA.");
            }

            var props = new byte[5];
            if (input.Read(props, 0, 5) != 5)
            {
                throw new ArgumentException("The input stream is not long enough to contain LZMA compressed data.");
            }

            dec.SetDecoderProperties(props);


            dec.Code(input, output, -1, DecompressLength, null);
        }
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Backup4.Compression;
using Backup4.Crypto;
using Backup4.Synchronization;

namespace Backup4.Processes
{
    public static class SingleFile
    {
        public static async Task Process(Stream input, Stream output, string password)
        {
            var comp = new LzmaCompressor {Level = 9};

            await Pipe.Connect(input, output, 1024 * 1024,
                (i, o) => comp.Compress(i, o),
                (i, o) => EasyEncrypt.Encrypt(i, o, password));
        }

        public static async Task Deprocess(Stream input, Stream output, string password)
        {
            var comp = new LzmaCompressor();
            
            await Pipe.Connect(input, output, 1024 * 1024,
                (i, o) => EasyEncrypt.Decrypt(i, o, password),
                (i, o) => comp.Decompress(i, o));
        }
    }
}
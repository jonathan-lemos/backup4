using System;
using System.IO;
using System.Threading.Tasks;
using Backup4.Compression;
using Backup4.Crypto;
using Backup4.Misc;

namespace Backup4.Processes
{
    public static class SingleFile
    {
        public static async Task Process(string inputFilename, Stream output, string password)
        {
            var comp = new LzmaCompressor {Level = 9};
            using var ips = new FileStream(inputFilename, FileMode.Open);

            await Pipe.Connect(ips, output, 1024 * 1024,
                (i, o) => comp.Compress(i, o),
                (i, o) => EasyEncrypt.Encrypt(i, o, password));
        }

        public static async Task Deprocess(string inputFilename, Stream output, string password)
        {
            using var ips = new FileStream(inputFilename, FileMode.Open);
            var comp = new LzmaCompressor();
            
            await Pipe.Connect(ips, output, 1024 * 1024,
                (i, o) => EasyEncrypt.Decrypt(i, o, password),
                (i, o) => comp.Decompress(i, o));
        }
    }
}
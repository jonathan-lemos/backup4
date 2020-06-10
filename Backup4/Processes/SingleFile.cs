using System;
using System.IO;
using System.Threading.Tasks;
using Backup4.Compression;
using Backup4.Crypto;
using Backup4.Functional;
using Backup4.Misc;
using Backup4.Synchronization;
using Org.BouncyCastle.Crypto.Parameters;

namespace Backup4.Processes
{
    public static class SingleFile
    {
        public static async Task<Result<AggregateException>> Process(Stream input, Stream output, string password)
        {
            var comp = new LzmaCompressor {Level = 9};

            var pipe = new Pipe(
                (i, o) => comp.Compress(i, o),
                (i, o) => EasyEncrypt.Encrypt(i, o, password)
            );
            pipe.SetInput(input);
            pipe.SetOutput(output);
            pipe.BufferSize = 1024 * 1024;

            return await pipe.Execute();
        }

        public static async Task<Result<AggregateException>> Deprocess(Stream input, Stream output, string password)
        {
            var comp = new LzmaCompressor();

            var pipe = new Pipe(
                (i, o) => EasyEncrypt.Decrypt(i, o, password),
                (i, o) => comp.Decompress(i, o)
            );
            
            pipe.SetInput(input);
            pipe.SetOutput(output);
            pipe.BufferSize = 1024 * 1024;

            return await pipe.Execute();
        }
    }
}
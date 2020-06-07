using System;
using System.IO;
using System.Threading.Tasks;
using Backup4.Processes;
using Backup4.Synchronization;
using SharpCompress.Archives;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Writers.Tar;

namespace Backup4
{
    public static class Backup
    {
        public static async Task Do(Options options)
        {
            await Pipe.Connect(async stream => await Tar.Make(stream, options.Directories), stream =>
            {
                var stdout = Console.OpenStandardOutput();
                var len = 0;
                var buf = new byte[65536];
                while ((len = stream.Read(buf, 0, buf.Length)) > 0)
                {
                    stdout.Write(buf, 0, buf.Length);
                }
            }, 1024 * 1024,
                );
        }
    }
}
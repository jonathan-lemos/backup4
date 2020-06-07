using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backup4.Filesystem;
using Backup4.Functional;
using Backup4.Synchronization;
using Mono.Unix.Native;
using Serilog;
using SharpCompress.Common;
using SharpCompress.Writers.Tar;

namespace Backup4.Processes
{
    public static class Tar
    {
        private static void WalkNamesRec(ConcurrentBuffer<(string Path, string Name, Stat stat)> buffer, string dir)
        {
            foreach (var res in Dir.Read(dir))
            {
                var (path, name, stat) = res;
                stat.Match(
                    val =>
                    {
                        buffer.Push((path, name, val));
                        if (val.IsDir())
                        {
                            WalkNamesRec(buffer, path);
                        }
                    },
                    err => Log.Warning("Failed to stat {path}: {reason}", path, err.Message)
                );
            }
        }

        public static async Task Make(Stream destination, IEnumerable<string> inputDirs)
        {
            var opt = new TarWriterOptions(CompressionType.None, true);
            var tarWriter = new TarWriter(destination, opt);
            var buffer = new ConcurrentBuffer<(string Path, string Name, Stat stat)>(4096);
            var cts = new CancellationTokenSource();

            var reader = Task.Run(() =>
            {
                inputDirs.ForEach(dir => WalkNamesRec(buffer, dir));
                cts.Cancel();
            });

            var writer = Task.Run(async () =>
            {
                var res = Option<(string Path, string Name, Stat stat)>.Empty;
                while ((res = buffer.Pop(cts.Token)).HasValue)
                {
                    var (path, name, stat) = res.Value;
                    if (stat.IsDir())
                    {
                        continue;
                    }

                    await Pipe.Connect(new FileStream(path, FileMode.Open), stream =>
                    {
                        tarWriter.Write(path, stream, null);
                    }, 65536);
                }
            });

            await Task.WhenAll(reader, writer);
        }
    }
}
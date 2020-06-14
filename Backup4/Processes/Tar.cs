using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backup4.Filesystem;
using Backup4.Functional;
using Backup4.Misc;
using Backup4.Models;
using Backup4.Synchronization;
using Microsoft.EntityFrameworkCore;
using Mono.Unix.Native;
using Serilog;
using SharpCompress.Common;
using SharpCompress.Writers.Tar;

namespace Backup4.Processes
{
    public static class Tar
    {
        private static bool WalkNamesRec(ConcurrentBuffer<(string Path, string Name, Stat stat)> buffer,
            BackupContext bc, DirectoryModel dir)
        {
            var changed = false;

            foreach (var res in Fs.ReadDir(dir.Path))
            {
                var (path, name, stat) = res;
                stat.Match(
                    val =>
                    {
                        if (val.IsDir())
                        {
                            var tmp = bc.Directories
                                .Include(x => x.Attributes)
                                .SingleOrDefault(x => x.Path == path);

                            if (tmp != null)
                            {
                                changed |= tmp.Attributes.SetStat(val);
                            }
                            else
                            {
                                var newDir = new DirectoryModel
                                {
                                    Attributes = new AttributesModel(),
                                    Path = path,
                                    Name = name
                                };
                                newDir.Attributes.SetStat(val);
                                bc.Directories.Add(newDir);
                                tmp = newDir;
                                changed = true;
                            }

                            changed |= WalkNamesRec(buffer, bc, tmp);
                        }
                        else if (val.IsFile())
                        {
                            var tmp = bc.Files
                                .Include(x => x.Attributes)
                                .SingleOrDefault(x => x.Directory.Path == dir.Path);

                            if (tmp != null)
                            {
                                changed |= tmp.Attributes.SetStat(val);
                            }
                            else
                            {
                                var newFile = new FileModel
                                {
                                    Attributes = new AttributesModel(),
                                    Directory = dir,
                                    Filename = name,
                                };
                                newFile.Attributes.SetStat(val);
                                bc.Files.Add(newFile);
                                changed = true;
                            }
                        }
                        else if (val.IsSymlink())
                        {
                            var tmp = bc.Symlinks
                                .Include(x => x.Attributes)
                                .SingleOrDefault(x => x.Directory.Path == dir.Path);

                            if (tmp != null)
                            {
                                changed |= tmp.Attributes.SetStat(val);
                            }
                            else
                            {
                                var newLink = new SymlinkModel
                                {
                                    Attributes = new AttributesModel(),
                                    Directory = dir,
                                    Filename = name,
                                };
                                newLink.Attributes.SetStat(val);
                                bc.Symlinks.Add(newLink);
                                changed = true;
                            }
                        }

                        buffer.Push((path, name, val));
                    },
                    err => Log.Warning("Failed to stat {path}: {reason}", path, err.Message)
                );
            }

            return changed;
        }

        public static async Task<Result<AggregateException>> Make(Stream destination, BackupContext bc,
            IEnumerable<string> inputDirs)
        {
            var opt = new TarWriterOptions(CompressionType.None, true);
            var tarWriter = new TarWriter(destination, opt);
            var buffer = new ConcurrentBuffer<(string Path, string Name, Stat stat)>(4096);
            var cts = new CancellationTokenSource();

            var inputDms = inputDirs.Select(dir =>
            {
                var tmp = bc.Directories
                    .Include(x => x.Attributes)
                    .SingleOrDefault(x => x.Path == dir);

                if (tmp == null)
                {
                    var (path, name, stat) = Fs.ReadEntry(dir);
                    if (!stat)
                    {
                        return new Result<DirectoryModel, FsException>(stat.Error);
                    }

                    tmp = new DirectoryModel
                    {
                        Attributes = new AttributesModel(),
                        Path = path,
                        Name = name
                    };
                    tmp.Attributes.SetStat(stat.Value);
                    bc.Directories.Add(tmp);
                }

                return tmp;
            }).Combine();

            if (!inputDms)
            {
                return inputDms.Error;
            }

            var reader = Task.Run(() => Result.Of(() =>
            {
                inputDms.Value.ForEach(dir => WalkNamesRec(buffer, bc, dir));
                cts.Cancel();
            }));

            var writer = Task.Run(async () =>
            {
                var res = Option<(string Path, string Name, Stat stat)>.Empty;
                while ((res = buffer.Pop(cts.Token)).HasValue)
                {
                    var (path, name, stat) = res.Value;
                    try
                    {
                        if (stat.IsDir())
                        {
                            continue;
                        }

                        await using var fs = new FileStream(path, FileMode.Open);

                        var pipe = new Pipe();
                        pipe.SetInput(fs);
                        pipe.SetOutput(stream => tarWriter.Write(path, stream, null));

                        (await pipe.Execute()).Match(
                            () => { },
                            err => Log.Warning("{path}: {err}", path, err.AllMessages())
                        );
                    }
                    catch (Exception e)
                    {
                        Log.Warning("{path}: {err}", path, e);
                    }
                }

                return Result<Exception>.Success;
            });
            return (await Task.WhenAll(reader, writer)).Combine();
        }
    }
}
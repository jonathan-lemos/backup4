using System.Collections.Generic;
using System.IO;
using Backup4.Functional;
using Backup4.Misc;
using Mono.Unix.Native;

namespace Backup4.Filesystem
{
    public static class Fs
    {
        public static (string Path, string Name, Result<Stat, FsException> Stat) ReadEntry(string path)
        {
            return ReadEntry(Paths.Directory(path), Paths.Filename(path));
        }

        public static (string Path, string Name, Result<Stat, FsException> Stat) ReadEntry(string dir, string name)
        {
            var full = Path.Join(Paths.Absolute(dir), name);

            if (Syscall.stat(full, out var stat) == 0)
            {
                if (stat.IsDir() && !full.EndsWith("/"))
                {
                    full += "/";
                }

                return (full, name, stat);
            }

            return (full, name, new FsException(Stdlib.GetLastError()));
        }

        public static IEnumerable<(string Path, string Name, Result<Stat, FsException> Stat)> ReadDir(string path)
        {
            foreach (var fname in Directory.EnumerateFileSystemEntries(path))
            {
                var fullPat = Path.Join(path, fname);
                if (Syscall.stat(fname, out var stat) != 0)
                {
                    yield return (fullPat, fname, new FsException(Stdlib.GetLastError()));
                    continue;
                }

                yield return (fullPat, fname, stat);
            }
        }
    }
}
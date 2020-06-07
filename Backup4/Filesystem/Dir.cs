using System.Collections.Generic;
using System.IO;
using Backup4.Functional;
using Mono.Unix.Native;

namespace Backup4.Filesystem
{
    public static class Dir
    {
        public static IEnumerable<(string Path, string Name, Result<Stat, FsException> Stat)> Read(string path)
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
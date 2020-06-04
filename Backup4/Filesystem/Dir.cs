using System.Collections.Generic;
using System.IO;
using Backup4.Functional;
using Mono.Unix.Native;

namespace Backup4.Filesystem
{
    public static class Dir
    {
        public static IEnumerable<Result<(string Path, string Name, Stat Stat), FsException>> Read(string path)
        {
            foreach (var fname in Directory.EnumerateFileSystemEntries(path))
            {
                var fullPat = Path.Join(path, fname);
                if (Syscall.stat(fname, out var stat) != 0)
                {
                    yield return new FsException(Stdlib.GetLastError());
                    continue;
                }

                yield return (fullPat, fname, stat);
            }
        }
    }
}
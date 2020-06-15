using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backup4.Filesystem;
using Mono.Unix.Native;

namespace Backup4Tests
{
    public class TempDir : IDisposable
    {
        public string Path { get; }
        public IReadOnlyList<(string Path, long Permissions)> Dirs { get; }
        public IReadOnlyList<(string Path, long Permissions)> Files { get; }

        public TempDir()
        {
            Path = System.IO.Path.Join(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            Dirs = Enumerable.Range(0, 3)
                .Select(i => System.IO.Path.Join(Path, $"tempdir{i}"))
                .Zip(new long[] {0755, 0400, 0000})
                .ToList();

            Files = Dirs.SelectMany(x =>
                    Enumerable.Range(0, 3)
                        .Select(i => System.IO.Path.Join(x.Path, $"tempfile{i}"))
                        .Zip(new long[] {0644, 0400, 0000}))
                .ToList();


            foreach (var (path, perms) in Files)
            {
                if (Syscall.chmod(path, (FilePermissions) perms) != 0)
                {
                    throw new FsException(path);
                }
            }
        }

        private void ReleaseUnmanagedResources()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~TempDir()
        {
            ReleaseUnmanagedResources();
        }
    }
}
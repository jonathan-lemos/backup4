using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backup4.Filesystem;
using Backup4.Misc;
using Mono.Unix.Native;

namespace Backup4Tests
{
    public class TempDir : IDisposable
    {
        public string Path { get; }
        public IReadOnlyList<(string Path, long Permissions)> Dirs { get; }
        public IReadOnlyList<(string Path, long Permissions)> Files { get; }
        public IEnumerable<(string Path, long Permissions)> Entries => Dirs.Concat(Files);

        public TempDir()
        {
            Path = System.IO.Path.Join(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());

            Dirs = Enumerable.Range(0, 3)
                .Select(i => System.IO.Path.Join(Path, $"tempdir{i}/"))
                .Zip(new long[] {0755, 0500, 0000}.Select(x => x.Octal()))
                .ToList();

            Files = Dirs.SelectMany(x =>
                    Enumerable.Range(0, 3)
                        .Select(i => System.IO.Path.Join(x.Path, $"tempfile{i}"))
                        .Zip(new long[] {0644, 0400, 0000}.Select(x => x.Octal())))
                .ToList();

            Dirs.ForEach(d => Directory.CreateDirectory(d.Path));

            Files.Zip(Enumerable.Range(0, Files.Count).Select(x => x * 1027)).ForEach(x =>
            {
                var (f, len) = x;
                var (path, permissions) = f;

                File.WriteAllBytes(path, Enumerable.Range(0, len).Select(x => (byte) x).ToArray());
            });


            foreach (var (path, perms) in Files.Concat(Dirs))
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
                var di = new DirectoryInfo(Path);
                di.EnumerateDirectories().ForEach(x => Syscall.chmod(x.FullName, (FilePermissions)0755.Octal()));
                di.EnumerateFiles().ForEach(x => Syscall.chmod(x.FullName, (FilePermissions)0666.Octal()));
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
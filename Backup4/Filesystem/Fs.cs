using System;
using System.Collections.Generic;
using System.IO;
using Backup4.Functional;
using Backup4.Misc;
using Mono.Unix.Native;

namespace Backup4.Filesystem
{
    public static class Fs
    {
        public static IntPtr NullPtr = (IntPtr)0x0;
        
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

            return (full, name, new FsException(full));
        }

        public static Result<IEnumerable<(string Path, string Name, Result<Stat, FsException> Stat)>, FsException>
            ReadDir(string path)
        {
            var dir = Syscall.opendir(path);
            if (dir == NullPtr)
            {
                return new Result<IEnumerable<(string Path, string Name, Result<Stat, FsException> Stat)>, FsException>(new FsException(path));
            }

            static IEnumerable<(string Path, string Name, Result<Stat, FsException> Stat)> Ret(IntPtr dir, string path)
            {
                var dirent = new Dirent();
                while (true)
                {
                    var res = Syscall.readdir_r(dir, dirent, out var resDirent);
                    if (res != 0)
                    {
                        throw new FsException("Failed to read directory entry", path);
                    }

                    if (resDirent == NullPtr)
                    {
                        break;
                    }

                    if (dirent.d_name == "." || dirent.d_name == "..")
                    {
                        continue;
                    }

                    var newPath = Path.Join(path, dirent.d_name);

                    if (Syscall.stat(newPath, out var stat) != 0)
                    {
                        yield return (newPath, dirent.d_name, new FsException("Failed to stat", path));
                    }

                    yield return (newPath, dirent.d_name, stat);
                }
            }

            return new Result<IEnumerable<(string Path, string Name, Result<Stat, FsException> Stat)>, FsException>(
                Ret(dir, path));
        }
    }
}
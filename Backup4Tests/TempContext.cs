using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Backup4.Filesystem;
using Backup4.Misc;
using Backup4.Models;
using Mono.Unix.Native;

namespace Backup4Tests
{
    public static class TempContext
    {
        public static DisposeWrapper<BackupContext> Create()
        {
            var tmpFile = new TempFile();
            var ctx = BackupContext.FromFile(tmpFile.Filename);
            ctx.Database.EnsureCreated();
            return new DisposeWrapper<BackupContext>(ctx, _ => tmpFile.Dispose());
        }

        public static DisposeWrapper<BackupContext> Create(IEnumerable<string> entries)
        {
            var tmpFile = new TempFile();
            var ctx = BackupContext.FromFile(tmpFile.Filename);
            ctx.Database.EnsureCreated();

            var resErrors = entries.Select(Fs.ReadEntry).ToList();
            var res = resErrors.Where(x => x.Stat)
                .Select(x => (x.Path, x.Name, Stat: x.Stat.Value)).ToList();

            var dirs = res.Where(x => x.Stat.IsDir()).ToList();
            var files = res.Where(x => x.Stat.IsFile()).ToList();
            var symlinks = res.Where(x => x.Stat.IsSymlink()).ToList();

            var dms = dirs.Select(d =>
            {
                var (path, name, stat) = d;
                var dm = new DirectoryModel
                {
                    Attributes = new AttributesModel(),
                    Name = name,
                    Path = path
                };
                dm.Attributes.SetStat(stat);
                ctx.Directories.Add(dm);

                return dm;
            }).ToList();

            foreach (var (path, name, stat) in files)
            {
                var parent = dms.FirstOrDefault(d => d.Path == Paths.Directory(path));
                if (parent == null)
                {
                    throw new FsException(path, "No parent directory found in context.", Errno.ENOENT);
                }

                var fm = new FileModel
                {
                    Attributes = new AttributesModel(),
                    Directory = parent,
                    Filename = name
                };
                fm.Attributes.SetStat(stat);
                ctx.Files.Add(fm);
            }

            foreach (var (path, name, stat) in symlinks)
            {
                var parent = dms.FirstOrDefault(d => d.Path == Paths.Directory(path));
                if (parent == null)
                {
                    throw new FsException(path, "No parent directory found in context.");
                }

                var sm = new SymlinkModel
                {
                    Attributes = new AttributesModel(),
                    Directory = parent,
                    Filename = name
                };
                sm.Attributes.SetStat(stat);
                ctx.Symlinks.Add(sm);
            }

            return new DisposeWrapper<BackupContext>(ctx, _ => tmpFile.Dispose());
        }
    }
}
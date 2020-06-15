using Backup4.Functional;
using Mono.Unix.Native;

namespace Backup4.Filesystem
{
    public static class StatExtensions
    {
        public static Result<string, FsException> GidToName(long gid)
        {
            var g = Syscall.getgrgid((uint)gid);
            if (g == null)
            {
                return new FsException($"Failed to get group information for gid {gid}");
            }

            return g.gr_name;
        }

        public static Result<long, FsException> NameToGid(string name)
        {
            var g = Syscall.getgrnam(name);
            if (g == null)
            {
                return new FsException($"Failed to get group information for '{name}'");
            }

            return g.gr_gid;
        }
        
         public static Result<string, FsException> UidToName(long uid)
         {
             var g = Syscall.getpwuid((uint)uid);
             if (g == null)
             {
                 return new FsException($"Failed to get passwd information for uid {uid}");
             }
 
             return g.pw_name;
         }
 
         public static Result<long, FsException> NameToUid(string name)
         {
             var g = Syscall.getpwnam(name);
             if (g == null)
             {
                 return new FsException($"Failed to get passwd information for {name}");
             }
 
             return g.pw_gid;
         }       
        public static bool IsDir(this Stat s) =>
            (s.st_mode & FilePermissions.S_IFMT) == FilePermissions.S_IFDIR;

        public static bool IsFile(this Stat s) =>
            (s.st_mode & FilePermissions.S_IFMT) == FilePermissions.S_IFREG;
        
        public static bool IsSymlink(this Stat s) =>
            (s.st_mode & FilePermissions.S_IFMT) == FilePermissions.S_IFLNK;
    }
}
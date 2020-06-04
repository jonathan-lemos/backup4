using Mono.Unix.Native;

namespace Backup4.Filesystem
{
    public static class StatExtensions
    {
        public static bool IsDir(this Stat s) =>
            (s.st_mode & FilePermissions.S_IFMT) == FilePermissions.S_IFDIR;

        public static bool IsFile(this Stat s) =>
            (s.st_mode & FilePermissions.S_IFMT) == FilePermissions.S_IFREG;
        
        public static bool IsSymlink(this Stat s) =>
            (s.st_mode & FilePermissions.S_IFMT) == FilePermissions.S_IFLNK;
    }
}
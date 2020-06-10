using System;
using System.Text.RegularExpressions;

namespace Backup4.Misc
{
    public static class Paths
    {
        private static readonly Regex PatRegex = new Regex(@"^(.*)/[^/]*$");
        private static readonly Regex FnRegex = new Regex(@"^.*/([^/]*)$");
        private static readonly Regex HomeRegex = new Regex(@"^(~/|~$)");

        private static readonly string HomeDir =
            (Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/").Replace("//", "/");

        public static string Filename(string path)
        {
            var match = FnRegex.Match(path);
            return !match.Success ? path : match.Groups[1].Value;
        }

        public static string Directory(string path)
        {
            var match = PatRegex.Match(path);
            return !match.Success ? path : match.Groups[1].Value;
        }

        public static string Absolute(string path)
        {
            if (path.StartsWith("/"))
            {
                return path;
            }

            return System.IO.Path.GetFullPath(HomeRegex.Replace(path, HomeDir));
        }
    }
}
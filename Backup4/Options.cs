using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Backup4
{
    public class Options
    {
        [Option(
            shortName: 'v',
            longName: "verbose",
            Default = false,
            HelpText = "Enables verbose logging to stderr.")]
        public bool Verbose { get; set; }
        
        [Option(
            shortName: 'd',
            longName: "directories",
            Default = new[] {"/"},
            HelpText = "The list of directories/files to back up.")]
        public string[] Directories { get; set; }
        
        [Option(
            shortName: 'e',
            longName: "exclude",
            Default = new[] {"^/sys", "^/proc", "^/tmp", "^/var/tmp", "cache", "node_modules", ".git"},
            HelpText = "A list of regular expressions to exclude.")]
        public string[] ExcludePatterns { get; set; }
        
        [Option(
            shortName: 'b',
            longName: "backup-dir",
            Default = "~/Backup4",
            HelpText = "The directory the backups should be put in.")]
        public string BackupDir { get; set; }

        public Options()
        {
            Verbose = false;
            Directories = new[] {"/"};
            ExcludePatterns = new[] {"^/sys", "^/proc", "^/tmp", "^/var/tmp", "cache", "node_modules", ".git"};
            BackupDir = "~/Backups";
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Options Deserialize(string s)
        {
            return JsonConvert.DeserializeObject<Options>(s);
        }
    }
}
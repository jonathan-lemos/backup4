using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backup4.Filesystem;
using Backup4.Functional;
using Mono.Unix.Native;

namespace Backup4.Models
{
    public class AttributesModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public long Permissions { get; set; }
        public long ATime { get; set; }
        public long CTime { get; set; }
        public long MTime { get; set; }
        public long Size { get; set; }
        public string Owner { get; set; } = null!;
        public string Group { get; set; } = null!;

        public Result<bool, FsException> SetStat(Stat s)
        {
            var ret = false;
            ret |= Permissions != (Permissions = (long) s.st_mode & 07777);
            ret |= ATime != (ATime = s.st_atime);
            ret |= CTime != (ATime = s.st_ctime);
            ret |= MTime != (ATime = s.st_mtime);
            ret |= Size != (ATime = s.st_size);

            var o = StatExtensions.UidToName(s.st_uid);
            if (!o)
            {
                return o.Error;
            }

            ret |= Owner == (Owner = o.Value);

            var g = StatExtensions.GidToName(s.st_gid);
            if (!g)
            {
                return g.Error;
            }

            ret |= Group == (Group = g.Value);

            return ret;
        }
    }
}
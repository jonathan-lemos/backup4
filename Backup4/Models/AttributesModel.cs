using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backup4.Models
{
    public class AttributesModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int Permissions { get; set; }
        public long ATime { get; set; }
        public long CTime { get; set; }
        public long MTime { get; set; }
        public long Size { get; set; }
        public string Owner { get; set; } = null!;
        public string Group { get; set; } = null!;
    }
}
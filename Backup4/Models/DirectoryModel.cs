using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backup4.Models
{
    public class DirectoryModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        
        public string Path { get; set; } = null!;
        public string Name { get; set; } = null!;
        
        public AttributesModel Attributes { get; set; } = null!;
    }
}
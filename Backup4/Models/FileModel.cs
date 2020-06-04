using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backup4.Models
{
    public class FileModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DirectoryModel Directory { get; set; } = null!;
        public int DirectoryId { get; set; }
        
        public string Filename { get; set; } = null!;
        
        public AttributesModel Attributes { get; set; } = null!;
    }
}
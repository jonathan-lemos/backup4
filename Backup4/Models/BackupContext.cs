using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace Backup4.Models
{
    public class BackupContext : DbContext
    {
        public DbSet<AttributesModel> Attributes { get; set; } = null!;
        public DbSet<DirectoryModel> Directories { get; set; } = null!;
        public DbSet<FileModel> Files { get; set; } = null!;
        public DbSet<SymlinkModel> Symlinks { get; set; } = null!;
        public DbSet<MetadataModel> Metadata { get; set; } = null!;

        public static BackupContext FromFile(string filename)
        {
            var opt = new DbContextOptionsBuilder<BackupContext>()
                .UseSqlite($"Data Source={filename}");

            return new BackupContext(opt.Options);
        }

        public BackupContext(DbContextOptions<BackupContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<FileModel>()
                .HasIndex(f => new {f.DirectoryId, f.Filename})
                .IsUnique();

            builder.Entity<DirectoryModel>()
                .HasIndex(d => d.Path)
                .IsUnique();
            
            builder.Entity<SymlinkModel>()
                .HasIndex(s => new {s.DirectoryId, s.Filename})
                .IsUnique();

            builder.Entity<MetadataModel>()
                .HasIndex(s => s.Key)
                .IsUnique();
        }
    }
}
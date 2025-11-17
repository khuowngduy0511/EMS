using ExamManagement.Common;
using ExamManagement.Models.Violation;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.ViolationService.Data;

public class ViolationDbContext : DbContext
{
    public ViolationDbContext(DbContextOptions<ViolationDbContext> options) : base(options)
    {
    }

    public DbSet<Violation> Violations { get; set; }
    public DbSet<ViolationKeyword> ViolationKeywords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Violation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<ViolationKeyword>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Keyword).IsUnique();
            entity.Property(e => e.Keyword).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Severity).IsRequired().HasMaxLength(50);
        });
    }
}


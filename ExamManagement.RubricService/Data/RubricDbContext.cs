using ExamManagement.Common;
using ExamManagement.Models.Rubric;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.RubricService.Data;

public class RubricDbContext : DbContext
{
    public RubricDbContext(DbContextOptions<RubricDbContext> options) : base(options)
    {
    }

    public DbSet<Rubric> Rubrics { get; set; }
    public DbSet<RubricItem> RubricItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Rubric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<RubricItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Rubric)
                .WithMany(r => r.Items)
                .HasForeignKey(e => e.RubricId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });
    }
}


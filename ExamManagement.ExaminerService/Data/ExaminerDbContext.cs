using ExamManagement.Common;
using ExamManagement.Models.Examiner;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.ExaminerService.Data;

public class ExaminerDbContext : DbContext
{
    public ExaminerDbContext(DbContextOptions<ExaminerDbContext> options) : base(options)
    {
    }

    public DbSet<Examiner> Examiners { get; set; }
    public DbSet<Assignment> Assignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Examiner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Examiner)
                .WithMany(ex => ex.Assignments)
                .HasForeignKey(e => e.ExaminerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        });
    }
}


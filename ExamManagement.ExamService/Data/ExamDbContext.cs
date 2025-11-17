using ExamManagement.Common;
using ExamManagement.Models.Exam;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.ExamService.Data;

public class ExamDbContext : DbContext
{
    public ExamDbContext(DbContextOptions<ExamDbContext> options) : base(options)
    {
    }

    public DbSet<Exam> Exams { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
        });
    }
}


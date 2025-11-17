using ExamManagement.Common;
using ExamManagement.Models.Subject;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.SubjectService.Data;

public class SubjectDbContext : DbContext
{
    public SubjectDbContext(DbContextOptions<SubjectDbContext> options) : base(options)
    {
    }

    public DbSet<Subject> Subjects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
        });
    }
}


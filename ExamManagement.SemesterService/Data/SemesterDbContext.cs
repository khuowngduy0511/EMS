using ExamManagement.Common;
using ExamManagement.Models.Semester;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.SemesterService.Data;

public class SemesterDbContext : DbContext
{
    public SemesterDbContext(DbContextOptions<SemesterDbContext> options) : base(options)
    {
    }

    public DbSet<Semester> Semesters { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
        });
    }
}


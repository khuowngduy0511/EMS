using ExamManagement.Common;
using ExamManagement.Models.Report;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.ReportService.Data;

public class ReportDbContext : DbContext
{
    public ReportDbContext(DbContextOptions<ReportDbContext> options) : base(options)
    {
    }

    public DbSet<Report> Reports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReportType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.GeneratedBy).IsRequired().HasMaxLength(100);
        });
    }
}


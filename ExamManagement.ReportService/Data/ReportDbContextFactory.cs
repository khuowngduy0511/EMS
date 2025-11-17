using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ExamManagement.Common;

namespace ExamManagement.ReportService.Data;

public class ReportDbContextFactory : IDesignTimeDbContextFactory<ReportDbContext>
{
    public ReportDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReportDbContext>();
        optionsBuilder.UseSqlServer(Constants.ConnectionStrings.ReportDB);

        return new ReportDbContext(optionsBuilder.Options);
    }
}


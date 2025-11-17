using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ExamManagement.Common;

namespace ExamManagement.ExamService.Data;

public class ExamDbContextFactory : IDesignTimeDbContextFactory<ExamDbContext>
{
    public ExamDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExamDbContext>();
        optionsBuilder.UseSqlServer(Constants.ConnectionStrings.ExamDB);

        return new ExamDbContext(optionsBuilder.Options);
    }
}


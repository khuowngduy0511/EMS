using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ExamManagement.Common;

namespace ExamManagement.SemesterService.Data;

public class SemesterDbContextFactory : IDesignTimeDbContextFactory<SemesterDbContext>
{
    public SemesterDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SemesterDbContext>();
        optionsBuilder.UseSqlServer(Constants.ConnectionStrings.SemesterDB);

        return new SemesterDbContext(optionsBuilder.Options);
    }
}


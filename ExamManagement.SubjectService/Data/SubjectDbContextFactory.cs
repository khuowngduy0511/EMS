using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ExamManagement.Common;

namespace ExamManagement.SubjectService.Data;

public class SubjectDbContextFactory : IDesignTimeDbContextFactory<SubjectDbContext>
{
    public SubjectDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SubjectDbContext>();
        optionsBuilder.UseSqlServer(Constants.ConnectionStrings.SubjectDB);

        return new SubjectDbContext(optionsBuilder.Options);
    }
}


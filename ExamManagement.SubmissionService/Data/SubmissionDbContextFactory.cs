using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ExamManagement.Common;

namespace ExamManagement.SubmissionService.Data;

public class SubmissionDbContextFactory : IDesignTimeDbContextFactory<SubmissionDbContext>
{
    public SubmissionDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SubmissionDbContext>();
        optionsBuilder.UseSqlServer(Constants.ConnectionStrings.SubmissionDB);

        return new SubmissionDbContext(optionsBuilder.Options);
    }
}


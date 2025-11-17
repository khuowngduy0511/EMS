using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ExamManagement.Common;

namespace ExamManagement.ExaminerService.Data;

public class ExaminerDbContextFactory : IDesignTimeDbContextFactory<ExaminerDbContext>
{
    public ExaminerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ExaminerDbContext>();
        optionsBuilder.UseSqlServer(Constants.ConnectionStrings.ExaminerDB);

        return new ExaminerDbContext(optionsBuilder.Options);
    }
}


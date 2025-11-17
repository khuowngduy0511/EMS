using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ExamManagement.Common;

namespace ExamManagement.RubricService.Data;

public class RubricDbContextFactory : IDesignTimeDbContextFactory<RubricDbContext>
{
    public RubricDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RubricDbContext>();
        optionsBuilder.UseSqlServer(Constants.ConnectionStrings.RubricDB);

        return new RubricDbContext(optionsBuilder.Options);
    }
}


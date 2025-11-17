using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ExamManagement.Common;

namespace ExamManagement.ViolationService.Data;

public class ViolationDbContextFactory : IDesignTimeDbContextFactory<ViolationDbContext>
{
    public ViolationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ViolationDbContext>();
        optionsBuilder.UseSqlServer(Constants.ConnectionStrings.ViolationDB);

        return new ViolationDbContext(optionsBuilder.Options);
    }
}


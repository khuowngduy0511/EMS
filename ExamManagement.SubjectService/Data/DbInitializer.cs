using ExamManagement.Models.Subject;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.SubjectService.Data;

public static class DbInitializer
{
    public static void Seed(SubjectDbContext context)
    {
        try
        {
            // Check if database is available
            if (!context.Database.CanConnect())
            {
                return; // Silently fail if database is not available
            }

            // Seed sample subjects
            var subjects = new List<Subject>
            {
                new Subject
                {
                    Name = "PRN222 - .NET Programming",
                    Code = "PRN222",
                    Description = "Lập trình .NET với ASP.NET Core",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };

            foreach (var subject in subjects)
            {
                // Use async method but wait for result in sync context
                var exists = context.Subjects.Any(s => s.Code == subject.Code);
                if (!exists)
                {
                    context.Subjects.Add(subject);
                }
            }

            context.SaveChanges();
        }
        catch (Exception)
        {
            // Silently fail - don't throw exceptions during seeding
            // This allows the service to continue running even if seeding fails
        }
    }
}


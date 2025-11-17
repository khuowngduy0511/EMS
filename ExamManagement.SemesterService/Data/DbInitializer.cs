using ExamManagement.Models.Semester;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.SemesterService.Data;

public static class DbInitializer
{
    public static void Seed(SemesterDbContext context)
    {
        // Seed sample semesters
        var currentYear = DateTime.Now.Year;
        var semesters = new List<Semester>
        {
            new Semester
            {
                Name = $"Fall {currentYear}",
                Code = $"FA{currentYear}",
                StartDate = new DateTime(currentYear, 9, 1),
                EndDate = new DateTime(currentYear, 12, 31),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Semester
            {
                Name = $"Spring {currentYear + 1}",
                Code = $"SP{currentYear + 1}",
                StartDate = new DateTime(currentYear + 1, 1, 1),
                EndDate = new DateTime(currentYear + 1, 4, 30),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var semester in semesters)
        {
            if (!context.Semesters.Any(s => s.Code == semester.Code))
            {
                context.Semesters.Add(semester);
            }
        }

        context.SaveChanges();
    }
}


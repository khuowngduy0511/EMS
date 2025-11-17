using ExamManagement.Models.Examiner;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.ExaminerService.Data;

public static class DbInitializer
{
    public static void Seed(ExaminerDbContext context)
    {
        // Seed examiners (users with Examiner role from AuthService)
        // Note: This assumes users are created in AuthService first in this order:
        // 1. admin (Admin role)
        // 2. manager (Manager role)
        // 3. moderator (Moderator role)
        // 4. examiner (Examiner role)
        // So examiner user ID should be 4
        
        var examiners = new List<Examiner>
        {
            new Examiner
            {
                UserId = 4, // Examiner user ID from AuthService (examiner username)
                Name = "Examiner User",
                Email = "examiner@example.com",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var examiner in examiners)
        {
            if (!context.Examiners.Any(e => e.UserId == examiner.UserId))
            {
                context.Examiners.Add(examiner);
            }
        }

        context.SaveChanges();
    }
}


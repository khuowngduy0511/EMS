using BCrypt.Net;
using ExamManagement.Common;
using ExamManagement.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.AuthService.Data;

public static class DbInitializer
{
    public static void Seed(AuthDbContext context)
    {
        // Ensure database is created
        if (!context.Database.CanConnect())
        {
            throw new InvalidOperationException("Cannot connect to database. Please ensure the database exists and migrations are applied.");
        }

        // Seed users for all roles
        var users = new List<User>
        {
            new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = Roles.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "manager",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123"),
                Role = Roles.Manager,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "moderator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("moderator123"),
                Role = Roles.Moderator,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "examiner",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("examiner123"),
                Role = Roles.Examiner,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Check existing users
        var existingUsers = context.Users.ToList();
        Console.WriteLine($"Existing users in database: {existingUsers.Count}");
        foreach (var existingUser in existingUsers)
        {
            Console.WriteLine($"  - {existingUser.Username} ({existingUser.Role})");
        }

        int addedCount = 0;
        foreach (var user in users)
        {
            var exists = context.Users.Any(u => u.Username == user.Username);
            if (!exists)
            {
                context.Users.Add(user);
                addedCount++;
                Console.WriteLine($"Adding user: {user.Username} ({user.Role})");
            }
            else
            {
                Console.WriteLine($"User already exists: {user.Username} ({user.Role})");
            }
        }

        if (addedCount > 0)
        {
            context.SaveChanges();
            Console.WriteLine($"Successfully seeded {addedCount} new users.");
        }
        else
        {
            Console.WriteLine("All users already exist in database. No new users were added.");
        }

        // Verify all roles exist
        var finalUsers = context.Users.ToList();
        var finalRoles = finalUsers.Select(u => u.Role).Distinct().OrderBy(r => r).ToList();
        var expectedRoles = users.Select(u => u.Role).Distinct().OrderBy(r => r).ToList();
        
        Console.WriteLine($"Final user count: {finalUsers.Count}");
        Console.WriteLine($"Final roles: {string.Join(", ", finalRoles)}");
        Console.WriteLine($"Expected roles: {string.Join(", ", expectedRoles)}");
        
        var missingRoles = expectedRoles.Except(finalRoles).ToList();
        if (missingRoles.Any())
        {
            throw new InvalidOperationException($"Missing roles in database: {string.Join(", ", missingRoles)}");
        }
    }
}

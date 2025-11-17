using ExamManagement.Models.Auth;
using System.Security.Claims;

namespace ExamManagement.AuthService.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}


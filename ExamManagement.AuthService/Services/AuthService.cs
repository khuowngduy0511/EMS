using BCrypt.Net;
using ExamManagement.AuthService.Data;
using ExamManagement.Common;
using ExamManagement.Contracts.Auth;
using ExamManagement.Models.Auth;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ExamManagement.AuthService.Services;

public class AuthService : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthDbContext context, 
        IJwtService jwtService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var token = _jwtService.GenerateToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            Expiry = DateTime.UtcNow.AddDays(Constants.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Role = user.Role,
            Username = user.Username,
            ExpiresAt = DateTime.UtcNow.AddMinutes(Constants.JwtExpirationMinutes)
        };
    }

    public async Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken 
                && !rt.IsRevoked 
                && rt.Expiry > DateTime.UtcNow
                && rt.User.IsActive);

        if (refreshToken == null)
        {
            return null;
        }

        // Revoke old refresh token
        refreshToken.IsRevoked = true;

        // Generate new tokens
        var newToken = _jwtService.GenerateToken(refreshToken.User);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = refreshToken.UserId,
            Token = newRefreshToken,
            Expiry = DateTime.UtcNow.AddDays(Constants.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            Role = refreshToken.User.Role,
            Username = refreshToken.User.Username,
            ExpiresAt = DateTime.UtcNow.AddMinutes(Constants.JwtExpirationMinutes)
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && !rt.IsRevoked);

        if (token == null)
        {
            return false;
        }

        token.IsRevoked = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<InviteUserResponse?> InviteUserAsync(InviteUserRequest request, int invitedBy)
    {
        // Validate role
        var validRoles = new[] { "Manager", "Moderator", "Examiner" };
        if (!validRoles.Contains(request.Role))
        {
            return null;
        }

        // Check if user already exists (only active users)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => (u.Email == request.Email || u.Username == request.Email) && u.IsActive);
        
        if (existingUser != null)
        {
            return null;
        }

        // Check if there's a pending invitation for this email
        var existingInvitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.Email == request.Email && !i.IsUsed && i.ExpiresAt > DateTime.Now);
        
        if (existingInvitation != null)
        {
            return null;
        }

        // Generate invitation token
        var token = GenerateInvitationToken();
        var invitation = new Invitation
        {
            Email = request.Email,
            Role = request.Role,
            Token = token,
            ExpiresAt = DateTime.Now.AddDays(7), // 7 days expiry
            InvitedBy = invitedBy,
            IsUsed = false
        };

        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();

        // Send invitation email
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:8001";
        var emailSent = await _emailService.SendInvitationEmailAsync(request.Email, request.Role, token, baseUrl);

        return new InviteUserResponse
        {
            InvitationId = invitation.Id,
            Email = request.Email,
            Role = request.Role,
            Message = emailSent 
                ? "Invitation sent successfully" 
                : "Invitation created but email could not be sent. Please check email configuration."
        };
    }

    public async Task<ValidateInviteTokenResponse> ValidateInviteTokenAsync(string token)
    {
        var invitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invitation == null)
        {
            return new ValidateInviteTokenResponse
            {
                Valid = false,
                Message = "Invalid invitation token"
            };
        }

        if (invitation.IsUsed)
        {
            return new ValidateInviteTokenResponse
            {
                Valid = false,
                Message = "This invitation has already been used"
            };
        }

        if (invitation.ExpiresAt < DateTime.Now)
        {
            return new ValidateInviteTokenResponse
            {
                Valid = false,
                Message = "This invitation has expired"
            };
        }

        return new ValidateInviteTokenResponse
        {
            Valid = true,
            Email = invitation.Email,
            Role = invitation.Role
        };
    }

    public async Task<AcceptInviteResponse> AcceptInviteAsync(AcceptInviteRequest request)
    {
        // Validate token
        var validation = await ValidateInviteTokenAsync(request.Token);
        if (!validation.Valid)
        {
            return new AcceptInviteResponse
            {
                Success = false,
                Message = validation.Message ?? "Invalid invitation token"
            };
        }

        // Validate password
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return new AcceptInviteResponse
            {
                Success = false,
                Message = "Password must be at least 6 characters long"
            };
        }

        if (request.Password != request.ConfirmPassword)
        {
            return new AcceptInviteResponse
            {
                Success = false,
                Message = "Passwords do not match"
            };
        }

        // Get invitation
        var invitation = await _context.Invitations
            .FirstOrDefaultAsync(i => i.Token == request.Token);

        if (invitation == null)
        {
            return new AcceptInviteResponse
            {
                Success = false,
                Message = "Invalid invitation token"
            };
        }

        // Check if user already exists (only active users)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => (u.Email == invitation.Email || u.Username == invitation.Email) && u.IsActive);
        
        if (existingUser != null)
        {
            return new AcceptInviteResponse
            {
                Success = false,
                Message = "A user with this email already exists"
            };
        }

        // Create user
        var username = invitation.Email.Split('@')[0]; // Use email prefix as username
        // Ensure username is unique
        var baseUsername = username;
        var counter = 1;
        while (await _context.Users.AnyAsync(u => u.Username == username))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        var user = new User
        {
            Username = username,
            Email = invitation.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = invitation.Role,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);

        // Mark invitation as used
        invitation.IsUsed = true;
        invitation.UsedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User created from invitation: {Email}, Role: {Role}, Username: {Username}", 
            invitation.Email, invitation.Role, username);

        return new AcceptInviteResponse
        {
            Success = true,
            Message = "Account created successfully",
            Username = username
        };
    }

    private string GenerateInvitationToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        var users = await _context.Users
            .OrderBy(u => u.Username)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return users;
    }

    public async Task<List<UserDto>> GetExaminersAsync()
    {
        var examiners = await _context.Users
            .Where(u => u.Role == Roles.Examiner && u.IsActive)
            .OrderBy(u => u.Username)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return examiners;
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            IsActive = user.IsActive
        };
    }

    public async Task<DeleteUserResponse> DeleteUserAsync(int userId, int deletedBy)
    {
        // Check if user exists
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return new DeleteUserResponse
            {
                Success = false,
                Message = "User not found"
            };
        }

        // Prevent deleting yourself
        if (user.Id == deletedBy)
        {
            return new DeleteUserResponse
            {
                Success = false,
                Message = "You cannot delete your own account"
            };
        }

        // Prevent deleting admin users (optional - you can remove this if you want to allow deleting admins)
        if (user.Role == "Admin")
        {
            return new DeleteUserResponse
            {
                Success = false,
                Message = "Cannot delete admin users"
            };
        }

        // Soft delete - set IsActive to false
        user.IsActive = false;
        user.UpdatedAt = DateTime.Now;

        // Revoke all refresh tokens for this user
        var refreshTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in refreshTokens)
        {
            token.IsRevoked = true;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} ({Username}) deleted by user {DeletedBy}", userId, user.Username, deletedBy);

        return new DeleteUserResponse
        {
            Success = true,
            Message = $"User '{user.Username}' has been deleted successfully"
        };
    }
}


using ExamManagement.Contracts.Auth;

namespace ExamManagement.AuthService.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<InviteUserResponse?> InviteUserAsync(InviteUserRequest request, int invitedBy);
    Task<ValidateInviteTokenResponse> ValidateInviteTokenAsync(string token);
    Task<AcceptInviteResponse> AcceptInviteAsync(AcceptInviteRequest request);
    Task<List<UserDto>> GetUsersAsync();
    Task<List<UserDto>> GetExaminersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<DeleteUserResponse> DeleteUserAsync(int userId, int deletedBy);
}


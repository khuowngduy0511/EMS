namespace ExamManagement.AuthService.Services;

public interface IEmailService
{
    Task<bool> SendInvitationEmailAsync(string email, string role, string invitationToken, string baseUrl);
}



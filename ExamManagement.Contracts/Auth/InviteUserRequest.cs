namespace ExamManagement.Contracts.Auth;

public class InviteUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Manager, Moderator, Examiner
}

public class InviteUserResponse
{
    public int InvitationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class AcceptInviteRequest
{
    public string Token { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AcceptInviteResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Username { get; set; }
}

public class ValidateInviteTokenResponse
{
    public bool Valid { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Message { get; set; }
}



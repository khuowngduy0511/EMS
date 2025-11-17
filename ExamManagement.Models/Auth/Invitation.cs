namespace ExamManagement.Models.Auth;

public class Invitation
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Manager, Moderator, Examiner
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public int InvitedBy { get; set; } // Admin user ID who sent the invitation
}



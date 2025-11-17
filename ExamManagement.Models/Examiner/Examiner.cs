namespace ExamManagement.Models.Examiner;

public class Examiner
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}

public class Assignment
{
    public int Id { get; set; }
    public int ExaminerId { get; set; }
    public int SubmissionId { get; set; }
    public int ExamId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed
    
    // Navigation properties
    public Examiner Examiner { get; set; } = null!;
}


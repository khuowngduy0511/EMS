namespace ExamManagement.Contracts.Examiner;

public class ExaminerDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class AssignmentDto
{
    public int Id { get; set; }
    public int ExaminerId { get; set; }
    public int SubmissionId { get; set; }
    public int ExamId { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AssignExaminerRequest
{
    public int ExaminerId { get; set; }
    public int SubmissionId { get; set; }
    public int ExamId { get; set; }
}

public class AssignMultipleSubmissionsRequest
{
    public int ExaminerId { get; set; }
    public List<int> SubmissionIds { get; set; } = new();
    public int ExamId { get; set; }
}

public class GradingProgressDto
{
    public int ExamId { get; set; }
    public int TotalAssignments { get; set; }
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public double CompletionRate { get; set; }
}


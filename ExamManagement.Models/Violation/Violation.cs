namespace ExamManagement.Models.Violation;

public class Violation
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string Type { get; set; } = string.Empty; // FileName, CodeDuplicate, Plagiarism, KeywordViolation, FileNameMatch
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsResolved { get; set; } = false;
}

public class ViolationKeyword
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}


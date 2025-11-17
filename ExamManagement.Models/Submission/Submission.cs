namespace ExamManagement.Models.Submission;

public class Submission
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int ExamId { get; set; }
    public string Status { get; set; } = "Submitted"; // Submitted, Grading, Graded, Rejected
    public decimal? TotalScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<SubmissionFile> Files { get; set; } = new List<SubmissionFile>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}

public class SubmissionFile
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // Word, Code, Image, Other
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Submission Submission { get; set; } = null!;
}

public class Grade
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int ExaminerId { get; set; }
    public string Scores { get; set; } = "{}"; // JSON string
    public string Comment { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Submission Submission { get; set; } = null!;
}


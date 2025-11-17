namespace ExamManagement.Contracts.Submission;

public class SubmissionDto
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int ExamId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SubmissionFileDto> Files { get; set; } = new();
    public List<GradeDto> Grades { get; set; } = new();
}

public class SubmissionFileDto
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GradeDto
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int ExaminerId { get; set; }
    public string Scores { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GradeSubmissionRequest
{
    public int SubmissionId { get; set; }
    public int ExaminerId { get; set; }
    public string Scores { get; set; } = "{}";
    public string Comment { get; set; } = string.Empty;
    public decimal TotalScore { get; set; }
}


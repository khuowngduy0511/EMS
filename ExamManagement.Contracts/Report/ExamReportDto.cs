namespace ExamManagement.Contracts.Report;

public class ExamReportDto
{
    public int SubmissionId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int ExamId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ViolationCount { get; set; }
    public bool HasViolations { get; set; }
}



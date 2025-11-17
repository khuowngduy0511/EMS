namespace ExamManagement.Models.Report;

public class Report
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string ReportType { get; set; } = string.Empty; // Exam, Violation, Grade
    public string Data { get; set; } = string.Empty; // JSON string
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = string.Empty;
}


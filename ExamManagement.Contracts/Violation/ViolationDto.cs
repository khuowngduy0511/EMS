namespace ExamManagement.Contracts.Violation;

public class ViolationDto
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsResolved { get; set; }
}

public class CheckViolationRequest
{
    public int SubmissionId { get; set; }
    public int ExamId { get; set; }
    public List<string> FileNames { get; set; } = new();
    public List<string> FilePaths { get; set; } = new();
}

public class ViolationKeywordDto
{
    public int Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateViolationKeywordRequest
{
    public string Keyword { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
}

public class UpdateViolationKeywordRequest
{
    public string Keyword { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public bool IsActive { get; set; } = true;
}

public class SubmissionWithViolationsDto
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public int ExamId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ViolationDto> Violations { get; set; } = new();
    public int ViolationCount { get; set; }
    public string HighestSeverity { get; set; } = string.Empty;
}

public class ConfirmZeroScoreRequest
{
    public int ExaminerId { get; set; }
    public string Comment { get; set; } = "Xác nhận 0 điểm do vi phạm quy định";
}


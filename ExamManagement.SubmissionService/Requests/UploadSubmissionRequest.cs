using Microsoft.AspNetCore.Http;

namespace ExamManagement.SubmissionService.Requests;

public class UploadSubmissionRequest
{
    public int ExamId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
}





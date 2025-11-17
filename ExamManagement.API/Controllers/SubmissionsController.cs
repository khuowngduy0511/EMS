using ExamManagement.Contracts.Submission;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubmissionsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SubmissionsController(IHttpClientFactory httpClientFactory, ILogger<SubmissionsController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("SubmissionService");
        _logger = logger;
    }

    [HttpPost("upload")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UploadSubmission(
        [FromForm] int examId, 
        [FromForm] string studentId, 
        [FromForm] string? studentName, 
        [FromForm] Microsoft.AspNetCore.Http.IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            using var content = new MultipartFormDataContent();
            using var fileStream = file.OpenReadStream();
            content.Add(new StreamContent(fileStream), "file", file.FileName);
            content.Add(new StringContent(examId.ToString()), "examId");
            content.Add(new StringContent(studentId), "studentId");
            content.Add(new StringContent(studentName ?? string.Empty), "studentName");

            var response = await _httpClient.PostAsync("/api/submissions/upload", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var submission = JsonSerializer.Deserialize<SubmissionDto>(responseContent, _jsonOptions);
                return Ok(submission);
            }
            return StatusCode((int)response.StatusCode, responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubmissionService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmissions([FromQuery] int? examId = null)
    {
        try
        {
            var url = "/api/submissions";
            if (examId.HasValue)
            {
                url += $"?examId={examId.Value}";
            }

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var submissions = JsonSerializer.Deserialize<List<SubmissionDto>>(content, _jsonOptions);
                return Ok(submissions);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubmissionService to get all submissions");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmission(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/submissions/{id}");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var submission = JsonSerializer.Deserialize<SubmissionDto>(content, _jsonOptions);
                return Ok(submission);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubmissionService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{submissionId}/files/{fileId}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(int submissionId, int fileId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/submissions/{submissionId}/files/{fileId}/download");
            var content = await response.Content.ReadAsByteArrayAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"file_{fileId}";
                return File(content, contentType, fileName);
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, errorContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId} from submission {SubmissionId}", fileId, submissionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{submissionId}/files/{fileId}/view")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ViewFile(int submissionId, int fileId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/submissions/{submissionId}/files/{fileId}/view");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/plain";
                return Content(content, contentType);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing file {FileId} from submission {SubmissionId}", fileId, submissionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{id}/grade")]
    [ProducesResponseType(typeof(GradeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GradeSubmission(int id, [FromBody] GradeSubmissionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/submissions/{id}/grade", request);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var grade = JsonSerializer.Deserialize<GradeDto>(content, _jsonOptions);
                return Ok(grade);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubmissionService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


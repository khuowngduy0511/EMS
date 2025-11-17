using ExamManagement.Contracts.Auth;
using ExamManagement.Contracts.Examiner;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExaminersController : ControllerBase
{
    private readonly HttpClient _examinerHttpClient;
    private readonly HttpClient _authHttpClient;
    private readonly ILogger<ExaminersController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ExaminersController(IHttpClientFactory httpClientFactory, ILogger<ExaminersController> logger)
    {
        _examinerHttpClient = httpClientFactory.CreateClient("ExaminerService");
        _authHttpClient = httpClientFactory.CreateClient("AuthService");
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ExaminerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExaminers()
    {
        try
        {
            // Get all users with Examiner role from AuthService (new endpoint that doesn't require Admin)
            var authResponse = await _authHttpClient.GetAsync("/api/auth/examiners");
            if (!authResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get examiners from AuthService. Status: {StatusCode}", authResponse.StatusCode);
                // Fallback to ExaminerService only
                var fallbackResponse = await _examinerHttpClient.GetAsync("/api/examiners");
                var fallbackContent = await fallbackResponse.Content.ReadAsStringAsync();
                if (fallbackResponse.IsSuccessStatusCode)
                {
                    var fallbackExaminers = JsonSerializer.Deserialize<List<ExaminerDto>>(fallbackContent, _jsonOptions);
                    return Ok(fallbackExaminers ?? new List<ExaminerDto>());
                }
                return StatusCode((int)fallbackResponse.StatusCode, fallbackContent);
            }

            var authContent = await authResponse.Content.ReadAsStringAsync();
            var examinerUsers = JsonSerializer.Deserialize<List<UserDto>>(authContent, _jsonOptions) ?? new List<UserDto>();
            
            _logger.LogInformation("Retrieved {Count} examiner users from AuthService", examinerUsers.Count);

            // Get examiners from ExaminerService to enrich data (optional - for additional info)
            var examinerServiceExaminers = new List<ExaminerDto>();
            try
            {
                var examinerServiceResponse = await _examinerHttpClient.GetAsync("/api/examiners");
                if (examinerServiceResponse.IsSuccessStatusCode)
                {
                    var examinerServiceContent = await examinerServiceResponse.Content.ReadAsStringAsync();
                    examinerServiceExaminers = JsonSerializer.Deserialize<List<ExaminerDto>>(examinerServiceContent, _jsonOptions) ?? new List<ExaminerDto>();
                    _logger.LogInformation("Retrieved {Count} examiners from ExaminerService", examinerServiceExaminers.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get examiners from ExaminerService, continuing with AuthService data only");
            }

            // Create examiner list from ALL users in AuthService, enriching with ExaminerService data if available
            var result = new List<ExaminerDto>();
            foreach (var user in examinerUsers)
            {
                var existingExaminer = examinerServiceExaminers.FirstOrDefault(e => e.UserId == user.Id);
                if (existingExaminer != null)
                {
                    // Use ExaminerService data if available (has more details)
                    result.Add(existingExaminer);
                }
                else
                {
                    // Create examiner DTO from user if not in ExaminerService
                    result.Add(new ExaminerDto
                    {
                        Id = 0, // Will be created when assigned
                        UserId = user.Id,
                        Name = user.Username,
                        Email = user.Email ?? $"{user.Username}@example.com",
                        CreatedAt = user.CreatedAt,
                        IsActive = user.IsActive
                    });
                }
            }

            _logger.LogInformation("Returning {Count} examiners total", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting examiners");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("assign")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignExaminer([FromBody] AssignExaminerRequest request)
    {
        try
        {
            var response = await _examinerHttpClient.PostAsJsonAsync("/api/examiners/assign", request);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var assignment = JsonSerializer.Deserialize<AssignmentDto>(content, _jsonOptions);
                return Ok(assignment);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ExaminerService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("assign-multiple")]
    [ProducesResponseType(typeof(List<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignMultipleSubmissions([FromBody] AssignMultipleSubmissionsRequest request)
    {
        try
        {
            var response = await _examinerHttpClient.PostAsJsonAsync("/api/examiners/assign-multiple", request);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var assignments = JsonSerializer.Deserialize<List<AssignmentDto>>(content, _jsonOptions);
                return Ok(assignments);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ExaminerService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("assignments")]
    [ProducesResponseType(typeof(List<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAssignments([FromQuery] int? examId = null, [FromQuery] int? examinerId = null)
    {
        try
        {
            var url = "/api/examiners/assignments";
            var queryParams = new List<string>();
            if (examId.HasValue) queryParams.Add($"examId={examId.Value}");
            if (examinerId.HasValue) queryParams.Add($"examinerId={examinerId.Value}");
            if (queryParams.Any()) url += "?" + string.Join("&", queryParams);
            
            var response = await _examinerHttpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var assignments = JsonSerializer.Deserialize<List<AssignmentDto>>(content, _jsonOptions);
                return Ok(assignments);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ExaminerService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("assignments/progress")]
    [ProducesResponseType(typeof(GradingProgressDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGradingProgress([FromQuery] int examId)
    {
        try
        {
            var response = await _examinerHttpClient.GetAsync($"/api/examiners/assignments/progress?examId={examId}");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var progress = JsonSerializer.Deserialize<GradingProgressDto>(content, _jsonOptions);
                return Ok(progress);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ExaminerService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


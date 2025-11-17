using ExamManagement.Contracts.Submission;
using ExamManagement.Contracts.Violation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ViolationsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ViolationsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ViolationsController(IHttpClientFactory httpClientFactory, ILogger<ViolationsController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ViolationService");
        _logger = logger;
    }

    [HttpGet("{submissionId}")]
    [ProducesResponseType(typeof(List<ViolationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetViolations(int submissionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/violations/{submissionId}");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var violations = JsonSerializer.Deserialize<List<ViolationDto>>(content, _jsonOptions);
                return Ok(violations);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ViolationService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("check")]
    [ProducesResponseType(typeof(List<ViolationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckViolation([FromBody] CheckViolationRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/violations/check", request);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var violations = JsonSerializer.Deserialize<List<ViolationDto>>(content, _jsonOptions);
                return Ok(violations);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ViolationService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("submissions")]
    [ProducesResponseType(typeof(List<SubmissionWithViolationsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmissionsWithViolations([FromQuery] int? examId = null)
    {
        try
        {
            var url = "/api/violations/submissions";
            if (examId.HasValue)
            {
                url += $"?examId={examId.Value}";
            }

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var submissions = JsonSerializer.Deserialize<List<SubmissionWithViolationsDto>>(content, _jsonOptions);
                return Ok(submissions);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ViolationService to get submissions with violations");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("submissions/{submissionId}/confirm-zero")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmZeroScore(int submissionId, [FromBody] ConfirmZeroScoreRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/violations/submissions/{submissionId}/confirm-zero", request);
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
            _logger.LogError(ex, "Error calling ViolationService to confirm zero score for submission {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


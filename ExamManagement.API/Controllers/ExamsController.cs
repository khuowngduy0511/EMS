using ExamManagement.Contracts.Exam;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExamsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ExamsController(IHttpClientFactory httpClientFactory, ILogger<ExamsController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ExamService");
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ExamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExams()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/exams");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var exams = JsonSerializer.Deserialize<List<ExamDto>>(content, _jsonOptions);
                return Ok(exams);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ExamService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExam(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/exams/{id}");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var exam = JsonSerializer.Deserialize<ExamDto>(content, _jsonOptions);
                return Ok(exam);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ExamService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExamDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/exams", request);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var exam = JsonSerializer.Deserialize<ExamDto>(content, _jsonOptions);
                return StatusCode((int)response.StatusCode, exam);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ExamService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateExam(int id, [FromBody] UpdateExamRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/exams/{id}", request);
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ExamService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


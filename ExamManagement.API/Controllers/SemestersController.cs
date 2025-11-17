using ExamManagement.Contracts.Semester;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SemestersController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SemestersController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SemestersController(IHttpClientFactory httpClientFactory, ILogger<SemestersController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("SemesterService");
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SemesterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSemesters()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/semesters");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var semesters = JsonSerializer.Deserialize<List<SemesterDto>>(content, _jsonOptions);
                return Ok(semesters);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SemesterService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(SemesterDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSemester([FromBody] CreateSemesterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/semesters", request);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var semester = JsonSerializer.Deserialize<SemesterDto>(content, _jsonOptions);
                return StatusCode((int)response.StatusCode, semester);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SemesterService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


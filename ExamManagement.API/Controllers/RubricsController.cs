using ExamManagement.Contracts.Rubric;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RubricsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RubricsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RubricsController(IHttpClientFactory httpClientFactory, ILogger<RubricsController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("RubricService");
        _logger = logger;
    }

    [HttpGet("{examId}")]
    [ProducesResponseType(typeof(RubricDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRubric(int examId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rubrics/{examId}");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var rubric = JsonSerializer.Deserialize<RubricDto>(content, _jsonOptions);
                return Ok(rubric);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling RubricService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("{examId}")]
    [ProducesResponseType(typeof(RubricDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRubric(int examId, [FromBody] CreateRubricRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/rubrics/{examId}", request);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var rubric = JsonSerializer.Deserialize<RubricDto>(content, _jsonOptions);
                return StatusCode((int)response.StatusCode, rubric);
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling RubricService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


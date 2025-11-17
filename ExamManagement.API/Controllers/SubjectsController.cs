using ExamManagement.Contracts.Subject;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubjectsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SubjectsController(IHttpClientFactory httpClientFactory, ILogger<SubjectsController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("SubjectService");
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubjects()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/subjects");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var subjects = JsonSerializer.Deserialize<List<SubjectDto>>(content, _jsonOptions);
                return Ok(subjects);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubjectService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubject(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/subjects/{id}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var subject = JsonSerializer.Deserialize<SubjectDto>(content, _jsonOptions);
                return Ok(subject);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubjectService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/subjects", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var subject = JsonSerializer.Deserialize<SubjectDto>(content, _jsonOptions);
                return StatusCode((int)response.StatusCode, subject);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubjectService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] UpdateSubjectRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/subjects/{id}", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, content);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubjectService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/subjects/{id}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, content);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling SubjectService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


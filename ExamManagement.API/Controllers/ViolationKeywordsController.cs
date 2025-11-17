using ExamManagement.Contracts.Violation;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/violation-keywords")]
public class ViolationKeywordsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ViolationKeywordsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ViolationKeywordsController(IHttpClientFactory httpClientFactory, ILogger<ViolationKeywordsController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ViolationService");
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ViolationKeywordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKeywords([FromQuery] bool? activeOnly = null)
    {
        try
        {
            var url = "/api/violation-keywords";
            if (activeOnly.HasValue)
            {
                url += $"?activeOnly={activeOnly.Value}";
            }

            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var keywords = JsonSerializer.Deserialize<List<ViolationKeywordDto>>(content, _jsonOptions);
                return Ok(keywords);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ViolationService to get keywords");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ViolationKeywordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKeyword(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/violation-keywords/{id}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var keyword = JsonSerializer.Deserialize<ViolationKeywordDto>(content, _jsonOptions);
                return Ok(keyword);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ViolationService to get keyword {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ViolationKeywordDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateKeyword([FromBody] CreateViolationKeywordRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/violation-keywords", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var keyword = JsonSerializer.Deserialize<ViolationKeywordDto>(content, _jsonOptions);
                return StatusCode((int)response.StatusCode, keyword);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ViolationService to create keyword");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ViolationKeywordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateKeyword(int id, [FromBody] UpdateViolationKeywordRequest request)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/violation-keywords/{id}", request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var keyword = JsonSerializer.Deserialize<ViolationKeywordDto>(content, _jsonOptions);
                return Ok(keyword);
            }
            
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ViolationService to update keyword {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteKeyword(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/violation-keywords/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                return NoContent();
            }
            
            var content = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ViolationService to delete keyword {Id}", id);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


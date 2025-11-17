using Microsoft.AspNetCore.Mvc;

namespace ExamManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IHttpClientFactory httpClientFactory, ILogger<ReportsController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ReportService");
        _logger = logger;
    }

    [HttpGet("exam")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> GetExamReports([FromQuery] int examId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/reports/exam?examId={examId}");
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                // Return JSON content as object for Swagger
                return Content(content, "application/json");
            }
            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ReportService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("exam/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/json")]
    public async Task<IActionResult> ExportExamReport([FromQuery] int examId)
    {
        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"/api/reports/exam/export?examId={examId}");
            // Forward Accept header from client request
            if (Request.Headers.ContainsKey("Accept"))
            {
                requestMessage.Headers.Add("Accept", Request.Headers["Accept"].ToString());
            }
            else
            {
                // Default to Excel format if no Accept header
                requestMessage.Headers.Add("Accept", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            }
            
            var response = await _httpClient.SendAsync(requestMessage);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"exam_{examId}_report.xlsx";
                return File(content, contentType, fileName);
            }
            var errorContent = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, errorContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ReportService");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}


using ExamManagement.Contracts.Submission;
using ExamManagement.Contracts.Violation;
using ExamManagement.Models.Violation;
using ExamManagement.ViolationService.Data;
using ExamManagement.ViolationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ExamManagement.ViolationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ViolationsController : ControllerBase
{
    private readonly IViolationService _violationService;
    private readonly ViolationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ViolationsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ViolationsController(
        IViolationService violationService,
        ViolationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<ViolationsController> logger)
    {
        _violationService = violationService;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("{submissionId}")]
    [Authorize(Roles = "Admin,Manager,Moderator,Examiner")]
    public async Task<ActionResult<List<ViolationDto>>> GetViolations(int submissionId)
    {
        var violations = await _violationService.GetViolationsAsync(submissionId);
        return Ok(violations);
    }

    [HttpPost("check")]
    [Authorize(Roles = "Admin,Manager,Moderator,Examiner")]
    public async Task<ActionResult<List<ViolationDto>>> CheckViolations(CheckViolationRequest request)
    {
        try
        {
            if (request.FileNames == null || request.FilePaths == null)
            {
                return BadRequest(new { message = "FileNames and FilePaths are required" });
            }

            if (request.FileNames.Count != request.FilePaths.Count)
            {
                return BadRequest(new { message = "FileNames and FilePaths must have the same count" });
            }

            var violations = await _violationService.CheckViolationsAsync(
                request.SubmissionId,
                request.ExamId,
                request.FileNames,
                request.FilePaths
            );

            var violationDtos = violations.Select(v => new ViolationDto
            {
                Id = v.Id,
                SubmissionId = v.SubmissionId,
                Type = v.Type,
                Description = v.Description,
                Severity = v.Severity,
                CreatedAt = v.CreatedAt,
                IsResolved = v.IsResolved
            }).ToList();

            return Ok(violationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking violations. SubmissionId: {SubmissionId}, ExamId: {ExamId}", 
                request?.SubmissionId, request?.ExamId);
            return StatusCode(500, new { message = "An error occurred while checking violations", error = ex.Message });
        }
    }

    [HttpGet("submissions")]
    [Authorize(Roles = "Admin,Manager,Moderator,Examiner")]
    [ProducesResponseType(typeof(List<SubmissionWithViolationsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubmissionWithViolationsDto>>> GetSubmissionsWithViolations([FromQuery] int? examId = null)
    {
        try
        {
            // Get all submissions with violations
            var violationsQuery = _context.Violations.AsQueryable();
            
            if (examId.HasValue)
            {
                // We need to get submission IDs from SubmissionService first
                // For now, we'll get all violations and filter by examId later
            }

            var violations = await violationsQuery
                .Where(v => !v.IsResolved) // Only unresolved violations
                .GroupBy(v => v.SubmissionId)
                .Select(g => new
                {
                    SubmissionId = g.Key,
                    Violations = g.Select(v => new ViolationDto
                    {
                        Id = v.Id,
                        SubmissionId = v.SubmissionId,
                        Type = v.Type,
                        Description = v.Description,
                        Severity = v.Severity,
                        CreatedAt = v.CreatedAt,
                        IsResolved = v.IsResolved
                    }).ToList(),
                    ViolationCount = g.Count(),
                    HighestSeverity = g.OrderByDescending(v => v.Severity == "Critical" ? 4 :
                                                               v.Severity == "High" ? 3 :
                                                               v.Severity == "Medium" ? 2 : 1)
                                       .First().Severity
                })
                .ToListAsync();

            if (!violations.Any())
            {
                return Ok(new List<SubmissionWithViolationsDto>());
            }

            // Get submission details from SubmissionService
            var submissionIds = violations.Select(v => v.SubmissionId).ToList();
            var submissionsWithViolations = new List<SubmissionWithViolationsDto>();

            var httpClient = _httpClientFactory.CreateClient("SubmissionService");
            var url = "/api/submissions";
            if (examId.HasValue)
            {
                url += $"?examId={examId.Value}";
            }

            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var allSubmissions = JsonSerializer.Deserialize<List<SubmissionDto>>(content, _jsonOptions);

                if (allSubmissions != null)
                {
                    // Filter submissions that have violations
                    var submissionsWithViols = allSubmissions
                        .Where(s => submissionIds.Contains(s.Id))
                        .ToList();

                    // Combine submission data with violations
                    foreach (var submission in submissionsWithViols)
                    {
                        var violationData = violations.FirstOrDefault(v => v.SubmissionId == submission.Id);
                        if (violationData != null)
                        {
                            submissionsWithViolations.Add(new SubmissionWithViolationsDto
                            {
                                Id = submission.Id,
                                StudentId = submission.StudentId,
                                StudentName = submission.StudentName,
                                ExamId = submission.ExamId,
                                Status = submission.Status,
                                TotalScore = submission.TotalScore,
                                CreatedAt = submission.CreatedAt,
                                Violations = violationData.Violations,
                                ViolationCount = violationData.ViolationCount,
                                HighestSeverity = violationData.HighestSeverity
                            });
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Failed to get submissions from SubmissionService. Status: {StatusCode}", response.StatusCode);
            }

            // Sort by highest severity first, then by violation count
            submissionsWithViolations = submissionsWithViolations
                .OrderByDescending(s => s.HighestSeverity == "Critical" ? 4 :
                                       s.HighestSeverity == "High" ? 3 :
                                       s.HighestSeverity == "Medium" ? 2 : 1)
                .ThenByDescending(s => s.ViolationCount)
                .ToList();

            return Ok(submissionsWithViolations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions with violations. ExamId: {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while retrieving submissions with violations", error = ex.Message });
        }
    }

    [HttpPost("submissions/{submissionId}/confirm-zero")]
    [Authorize(Roles = "Admin,Manager,Moderator")] // Allow Admin, Manager, Moderator to confirm zero score
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubmissionDto>> ConfirmZeroScore(int submissionId, [FromBody] ConfirmZeroScoreRequest request)
    {
        try
        {
            // Check if submission has violations
            var hasViolations = await _context.Violations
                .AnyAsync(v => v.SubmissionId == submissionId && !v.IsResolved);

            if (!hasViolations)
            {
                return BadRequest(new { message = "Submission does not have any unresolved violations" });
            }

            // Call SubmissionService to grade with 0 points
            var httpClient = _httpClientFactory.CreateClient("SubmissionService");
            var gradeRequest = new GradeSubmissionRequest
            {
                SubmissionId = submissionId,
                ExaminerId = request.ExaminerId,
                Scores = "{}",
                Comment = request.Comment ?? "Xác nhận 0 điểm do vi phạm quy định",
                TotalScore = 0
            };

            var response = await httpClient.PostAsJsonAsync($"/api/submissions/{submissionId}/grade", gradeRequest);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var submission = JsonSerializer.Deserialize<SubmissionDto>(content, _jsonOptions);
                _logger.LogInformation("Zero score confirmed for submission {SubmissionId} by examiner {ExaminerId}", 
                    submissionId, request.ExaminerId);
                return Ok(submission);
            }

            return StatusCode((int)response.StatusCode, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming zero score for submission {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "An error occurred while confirming zero score", error = ex.Message });
        }
    }
}

[ApiController]
[Route("api/violation-keywords")]
public class ViolationKeywordsController : ControllerBase
{
    private readonly ViolationDbContext _context;
    private readonly ILogger<ViolationKeywordsController> _logger;

    public ViolationKeywordsController(ViolationDbContext context, ILogger<ViolationKeywordsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<List<ViolationKeywordDto>>> GetKeywords([FromQuery] bool? activeOnly = null)
    {
        try
        {
            var query = _context.ViolationKeywords.AsQueryable();
            
            if (activeOnly == true)
            {
                query = query.Where(k => k.IsActive);
            }

            var keywords = await query
                .OrderBy(k => k.Keyword)
                .Select(k => new ViolationKeywordDto
                {
                    Id = k.Id,
                    Keyword = k.Keyword,
                    Description = k.Description,
                    Severity = k.Severity,
                    IsActive = k.IsActive,
                    CreatedAt = k.CreatedAt
                })
                .ToListAsync();

            return Ok(keywords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving violation keywords");
            return StatusCode(500, new { message = "An error occurred while retrieving keywords", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ViolationKeywordDto>> GetKeyword(int id)
    {
        try
        {
            var keyword = await _context.ViolationKeywords.FindAsync(id);
            
            if (keyword == null)
            {
                return NotFound(new { message = "Keyword not found" });
            }

            return Ok(new ViolationKeywordDto
            {
                Id = keyword.Id,
                Keyword = keyword.Keyword,
                Description = keyword.Description,
                Severity = keyword.Severity,
                IsActive = keyword.IsActive,
                CreatedAt = keyword.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving violation keyword {Id}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving keyword", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ViolationKeywordDto>> CreateKeyword([FromBody] CreateViolationKeywordRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Keyword))
            {
                return BadRequest(new { message = "Keyword is required" });
            }

            // Check if keyword already exists
            var existing = await _context.ViolationKeywords
                .FirstOrDefaultAsync(k => k.Keyword.ToLower() == request.Keyword.ToLower());
            
            if (existing != null)
            {
                return Conflict(new { message = $"Keyword '{request.Keyword}' already exists" });
            }

            var keyword = new ViolationKeyword
            {
                Keyword = request.Keyword.Trim(),
                Description = request.Description ?? string.Empty,
                Severity = request.Severity ?? "Medium",
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.ViolationKeywords.Add(keyword);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetKeyword), new { id = keyword.Id }, new ViolationKeywordDto
            {
                Id = keyword.Id,
                Keyword = keyword.Keyword,
                Description = keyword.Description,
                Severity = keyword.Severity,
                IsActive = keyword.IsActive,
                CreatedAt = keyword.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating violation keyword");
            return StatusCode(500, new { message = "An error occurred while creating keyword", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<ViolationKeywordDto>> UpdateKeyword(int id, [FromBody] UpdateViolationKeywordRequest request)
    {
        try
        {
            var keyword = await _context.ViolationKeywords.FindAsync(id);
            
            if (keyword == null)
            {
                return NotFound(new { message = "Keyword not found" });
            }

            if (string.IsNullOrWhiteSpace(request.Keyword))
            {
                return BadRequest(new { message = "Keyword is required" });
            }

            // Check if keyword already exists (excluding current)
            var existing = await _context.ViolationKeywords
                .FirstOrDefaultAsync(k => k.Keyword.ToLower() == request.Keyword.ToLower() && k.Id != id);
            
            if (existing != null)
            {
                return Conflict(new { message = $"Keyword '{request.Keyword}' already exists" });
            }

            keyword.Keyword = request.Keyword.Trim();
            keyword.Description = request.Description ?? string.Empty;
            keyword.Severity = request.Severity ?? "Medium";
            keyword.IsActive = request.IsActive;
            keyword.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new ViolationKeywordDto
            {
                Id = keyword.Id,
                Keyword = keyword.Keyword,
                Description = keyword.Description,
                Severity = keyword.Severity,
                IsActive = keyword.IsActive,
                CreatedAt = keyword.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating violation keyword {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating keyword", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteKeyword(int id)
    {
        try
        {
            var keyword = await _context.ViolationKeywords.FindAsync(id);
            
            if (keyword == null)
            {
                return NotFound(new { message = "Keyword not found" });
            }

            _context.ViolationKeywords.Remove(keyword);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting violation keyword {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting keyword", error = ex.Message });
        }
    }
}


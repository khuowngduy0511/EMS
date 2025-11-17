using ExamManagement.Common;
using ExamManagement.Contracts.Examiner;
using ExamManagement.Models.Examiner;
using ExamManagement.ExaminerService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.ExaminerService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExaminersController : ControllerBase
{
    private readonly ExaminerDbContext _context;
    private readonly ILogger<ExaminersController> _logger;

    public ExaminersController(ExaminerDbContext context, ILogger<ExaminersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Moderator,Examiner")]
    public async Task<ActionResult<IEnumerable<ExaminerDto>>> GetExaminers()
    {
        var examiners = await _context.Examiners
            .Where(e => e.IsActive)
            .Select(e => new ExaminerDto
            {
                Id = e.Id,
                UserId = e.UserId,
                Name = e.Name,
                Email = e.Email,
                CreatedAt = e.CreatedAt,
                IsActive = e.IsActive
            })
            .ToListAsync();

        return Ok(examiners);
    }

    [HttpPost("assign")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(AssignmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssignmentDto>> AssignExaminer(AssignExaminerRequest request)
    {
        try
        {
            // Check if assignment already exists
            var existingAssignment = await _context.Assignments
                .FirstOrDefaultAsync(a => a.SubmissionId == request.SubmissionId && 
                                         a.ExaminerId == request.ExaminerId &&
                                         a.Status != "Completed");

            if (existingAssignment != null)
            {
                return BadRequest(new { message = "This examiner is already assigned to this submission" });
            }

            var assignment = new Assignment
            {
                ExaminerId = request.ExaminerId,
                SubmissionId = request.SubmissionId,
                ExamId = request.ExamId,
                Status = "Pending",
                AssignedAt = DateTime.Now
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Examiner {ExaminerId} assigned to submission {SubmissionId} by Manager", 
                request.ExaminerId, request.SubmissionId);

            return CreatedAtAction(nameof(GetExaminers), new { id = assignment.Id }, new AssignmentDto
            {
                Id = assignment.Id,
                ExaminerId = assignment.ExaminerId,
                SubmissionId = assignment.SubmissionId,
                ExamId = assignment.ExamId,
                AssignedAt = assignment.AssignedAt,
                Status = assignment.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning examiner to submission");
            return StatusCode(500, new { message = "An error occurred while assigning examiner", error = ex.Message });
        }
    }

    [HttpPost("assign-multiple")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(List<AssignmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<AssignmentDto>>> AssignMultipleSubmissions([FromBody] AssignMultipleSubmissionsRequest request)
    {
        try
        {
            if (request.SubmissionIds == null || !request.SubmissionIds.Any())
            {
                return BadRequest(new { message = "At least one submission ID is required" });
            }

            var assignments = new List<Assignment>();
            var errors = new List<string>();

            foreach (var submissionId in request.SubmissionIds)
            {
                // Check if assignment already exists
                var existingAssignment = await _context.Assignments
                    .FirstOrDefaultAsync(a => a.SubmissionId == submissionId && 
                                             a.ExaminerId == request.ExaminerId &&
                                             a.Status != "Completed");

                if (existingAssignment != null)
                {
                    errors.Add($"Submission {submissionId} is already assigned to this examiner");
                    continue;
                }

                var assignment = new Assignment
                {
                    ExaminerId = request.ExaminerId,
                    SubmissionId = submissionId,
                    ExamId = request.ExamId,
                    Status = "Pending",
                    AssignedAt = DateTime.Now
                };

                assignments.Add(assignment);
            }

            if (!assignments.Any())
            {
                return BadRequest(new { message = "No new assignments could be created", errors = errors });
            }

            _context.Assignments.AddRange(assignments);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Examiner {ExaminerId} assigned to {Count} submissions by Manager", 
                request.ExaminerId, assignments.Count);

            var result = assignments.Select(a => new AssignmentDto
            {
                Id = a.Id,
                ExaminerId = a.ExaminerId,
                SubmissionId = a.SubmissionId,
                ExamId = a.ExamId,
                AssignedAt = a.AssignedAt,
                Status = a.Status
            }).ToList();

            return CreatedAtAction(nameof(GetExaminers), result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning examiner to multiple submissions");
            return StatusCode(500, new { message = "An error occurred while assigning examiner", error = ex.Message });
        }
    }

    [HttpGet("assignments")]
    [Authorize(Roles = "Manager,Moderator")]
    [ProducesResponseType(typeof(List<AssignmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AssignmentDto>>> GetAssignments([FromQuery] int? examId = null, [FromQuery] int? examinerId = null)
    {
        try
        {
            var query = _context.Assignments
                .Include(a => a.Examiner)
                .AsQueryable();

            if (examId.HasValue)
            {
                query = query.Where(a => a.ExamId == examId.Value);
            }

            if (examinerId.HasValue)
            {
                query = query.Where(a => a.ExaminerId == examinerId.Value);
            }

            var assignments = await query
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => new AssignmentDto
                {
                    Id = a.Id,
                    ExaminerId = a.ExaminerId,
                    SubmissionId = a.SubmissionId,
                    ExamId = a.ExamId,
                    AssignedAt = a.AssignedAt,
                    CompletedAt = a.CompletedAt,
                    Status = a.Status
                })
                .ToListAsync();

            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignments");
            return StatusCode(500, new { message = "An error occurred while retrieving assignments", error = ex.Message });
        }
    }

    [HttpGet("assignments/progress")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(GradingProgressDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GradingProgressDto>> GetGradingProgress([FromQuery] int examId)
    {
        try
        {
            var assignments = await _context.Assignments
                .Where(a => a.ExamId == examId)
                .ToListAsync();

            var total = assignments.Count;
            var pending = assignments.Count(a => a.Status == "Pending");
            var inProgress = assignments.Count(a => a.Status == "InProgress");
            var completed = assignments.Count(a => a.Status == "Completed");

            var progress = new GradingProgressDto
            {
                ExamId = examId,
                TotalAssignments = total,
                Pending = pending,
                InProgress = inProgress,
                Completed = completed,
                CompletionRate = total > 0 ? (double)completed / total * 100 : 0
            };

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving grading progress for exam {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while retrieving grading progress", error = ex.Message });
        }
    }
}


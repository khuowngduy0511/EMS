using ExamManagement.Common;
using ExamManagement.Contracts.Subject;
using ExamManagement.Models.Subject;
using ExamManagement.SubjectService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.SubjectService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubjectsController : ControllerBase
{
    private readonly SubjectDbContext _context;
    private readonly ILogger<SubjectsController> _logger;

    public SubjectsController(SubjectDbContext context, ILogger<SubjectsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous] // All authenticated users can view subjects
    public async Task<ActionResult<IEnumerable<SubjectDto>>> GetSubjects()
    {
        try
        {
            var subjects = await _context.Subjects
                .Where(s => s.IsActive)
                .Select(s => new SubjectDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    IsActive = s.IsActive
                })
                .ToListAsync();

            return Ok(subjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subjects");
            return StatusCode(500, new { message = "An error occurred while retrieving subjects", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous] // All authenticated users can view subject details
    public async Task<ActionResult<SubjectDto>> GetSubject(int id)
    {
        try
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null || !subject.IsActive)
            {
                return NotFound(new { message = $"Subject with id {id} not found" });
            }

            return Ok(new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code,
                Description = subject.Description,
                CreatedAt = subject.CreatedAt,
                IsActive = subject.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subject with id {SubjectId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the subject", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SubjectDto>> CreateSubject(CreateSubjectRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Subject name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { message = "Subject code is required" });
            }

            // Check if code already exists
            var existingSubject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Code == request.Code && s.IsActive);
            
            if (existingSubject != null)
            {
                return Conflict(new { message = $"Subject with code {request.Code} already exists" });
            }

            var subject = new Subject
            {
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code,
                Description = subject.Description,
                CreatedAt = subject.CreatedAt,
                IsActive = subject.IsActive
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while creating subject");
            return StatusCode(500, new { message = "An error occurred while creating the subject", error = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subject");
            return StatusCode(500, new { message = "An error occurred while creating the subject", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSubject(int id, UpdateSubjectRequest request)
    {
        try
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                return NotFound(new { message = $"Subject with id {id} not found" });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Subject name is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { message = "Subject code is required" });
            }

            // Check if code already exists for another subject
            var existingSubject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Code == request.Code && s.Id != id && s.IsActive);
            
            if (existingSubject != null)
            {
                return Conflict(new { message = $"Subject with code {request.Code} already exists" });
            }

            subject.Name = request.Name;
            subject.Code = request.Code;
            subject.Description = request.Description;
            subject.IsActive = request.IsActive;
            subject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while updating subject with id {SubjectId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the subject", error = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subject with id {SubjectId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the subject", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        try
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                return NotFound(new { message = $"Subject with id {id} not found" });
            }

            subject.IsActive = false;
            subject.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while deleting subject with id {SubjectId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the subject", error = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subject with id {SubjectId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the subject", error = ex.Message });
        }
    }
}


using ExamManagement.Common;
using ExamManagement.Contracts.Exam;
using ExamManagement.Models.Exam;
using ExamManagement.ExamService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.ExamService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly ExamDbContext _context;

    public ExamsController(ExamDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous] // Allow all authenticated users to view exams
    public async Task<ActionResult<IEnumerable<ExamDto>>> GetExams()
    {
        var exams = await _context.Exams
            .Where(e => e.IsActive)
            .Select(e => new ExamDto
            {
                Id = e.Id,
                Name = e.Name,
                Code = e.Code,
                SubjectId = e.SubjectId,
                SemesterId = e.SemesterId,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                CreatedAt = e.CreatedAt,
                IsActive = e.IsActive
            })
            .ToListAsync();

        return Ok(exams);
    }

    [HttpGet("{id}")]
    [AllowAnonymous] // Allow all authenticated users to view exam details
    public async Task<ActionResult<ExamDto>> GetExam(int id)
    {
        var exam = await _context.Exams.FindAsync(id);

        if (exam == null || !exam.IsActive)
        {
            return NotFound();
        }

        return Ok(new ExamDto
        {
            Id = exam.Id,
            Name = exam.Name,
            Code = exam.Code,
            SubjectId = exam.SubjectId,
            SemesterId = exam.SemesterId,
            Description = exam.Description,
            StartDate = exam.StartDate,
            EndDate = exam.EndDate,
            CreatedAt = exam.CreatedAt,
            IsActive = exam.IsActive
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ExamDto>> CreateExam(CreateExamRequest request)
    {
        var exam = new Exam
        {
            Name = request.Name,
            Code = request.Code,
            SubjectId = request.SubjectId,
            SemesterId = request.SemesterId,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetExam), new { id = exam.Id }, new ExamDto
        {
            Id = exam.Id,
            Name = exam.Name,
            Code = exam.Code,
            SubjectId = exam.SubjectId,
            SemesterId = exam.SemesterId,
            Description = exam.Description,
            StartDate = exam.StartDate,
            EndDate = exam.EndDate,
            CreatedAt = exam.CreatedAt,
            IsActive = exam.IsActive
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateExam(int id, UpdateExamRequest request)
    {
        var exam = await _context.Exams.FindAsync(id);

        if (exam == null)
        {
            return NotFound();
        }

        exam.Name = request.Name;
        exam.Code = request.Code;
        exam.SubjectId = request.SubjectId;
        exam.SemesterId = request.SemesterId;
        exam.Description = request.Description;
        exam.StartDate = request.StartDate;
        exam.EndDate = request.EndDate;
        exam.IsActive = request.IsActive;
        exam.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}


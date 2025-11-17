using ExamManagement.Common;
using ExamManagement.Contracts.Semester;
using ExamManagement.Models.Semester;
using ExamManagement.SemesterService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.SemesterService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SemestersController : ControllerBase
{
    private readonly SemesterDbContext _context;

    public SemestersController(SemesterDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous] // Allow all authenticated users to view semesters
    public async Task<ActionResult<IEnumerable<SemesterDto>>> GetSemesters()
    {
        var semesters = await _context.Semesters
            .Where(s => s.IsActive)
            .Select(s => new SemesterDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                CreatedAt = s.CreatedAt,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(semesters);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<SemesterDto>> CreateSemester(CreateSemesterRequest request)
    {
        var semester = new Semester
        {
            Name = request.Name,
            Code = request.Code,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Semesters.Add(semester);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSemesters), new { id = semester.Id }, new SemesterDto
        {
            Id = semester.Id,
            Name = semester.Name,
            Code = semester.Code,
            StartDate = semester.StartDate,
            EndDate = semester.EndDate,
            CreatedAt = semester.CreatedAt,
            IsActive = semester.IsActive
        });
    }
}


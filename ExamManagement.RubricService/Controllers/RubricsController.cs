using ExamManagement.Common;
using ExamManagement.Contracts.Rubric;
using ExamManagement.Models.Rubric;
using ExamManagement.RubricService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExamManagement.RubricService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RubricsController : ControllerBase
{
    private readonly RubricDbContext _context;

    public RubricsController(RubricDbContext context)
    {
        _context = context;
    }

    [HttpGet("{examId}")]
    [Authorize(Roles = "Admin,Manager,Moderator,Examiner")]
    public async Task<ActionResult<RubricDto>> GetRubric(int examId)
    {
        var rubric = await _context.Rubrics
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.ExamId == examId);

        if (rubric == null)
        {
            return NotFound();
        }

        return Ok(new RubricDto
        {
            Id = rubric.Id,
            ExamId = rubric.ExamId,
            Name = rubric.Name,
            Description = rubric.Description,
            TotalPoints = rubric.TotalPoints,
            Items = rubric.Items.Select(i => new RubricItemDto
            {
                Id = i.Id,
                RubricId = i.RubricId,
                Name = i.Name,
                Description = i.Description,
                Points = i.Points,
                Order = i.Order
            }).ToList()
        });
    }

    [HttpPost("{examId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<RubricDto>> CreateRubric(int examId, CreateRubricRequest request)
    {
        var rubric = new Rubric
        {
            ExamId = examId,
            Name = request.Name,
            Description = request.Description,
            TotalPoints = request.Items.Sum(i => i.Points),
            CreatedAt = DateTime.UtcNow
        };

        _context.Rubrics.Add(rubric);
        await _context.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            var rubricItem = new RubricItem
            {
                RubricId = rubric.Id,
                Name = item.Name,
                Description = item.Description,
                Points = item.Points,
                Order = item.Order,
                CreatedAt = DateTime.UtcNow
            };
            _context.RubricItems.Add(rubricItem);
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRubric), new { examId = rubric.ExamId }, new RubricDto
        {
            Id = rubric.Id,
            ExamId = rubric.ExamId,
            Name = rubric.Name,
            Description = rubric.Description,
            TotalPoints = rubric.TotalPoints,
            Items = request.Items.Select((i, idx) => new RubricItemDto
            {
                Name = i.Name,
                Description = i.Description,
                Points = i.Points,
                Order = i.Order
            }).ToList()
        });
    }
}


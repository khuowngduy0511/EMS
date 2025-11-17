using ExamManagement.Common;
using ExamManagement.Contracts.Examiner;
using ExamManagement.Contracts.Submission;
using ExamManagement.Models.Submission;
using ExamManagement.SubmissionService.Data;
using ExamManagement.SubmissionService.Requests;
using ExamManagement.SubmissionService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace ExamManagement.SubmissionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly SubmissionDbContext _context;
    private readonly IFileService _fileService;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SubmissionsController> _logger;

    public SubmissionsController(
        SubmissionDbContext context,
        IFileService fileService,
        IWebHostEnvironment environment,
        IHttpClientFactory httpClientFactory,
        ILogger<SubmissionsController> logger)
    {
        _context = context;
        _fileService = fileService;
        _environment = environment;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [AllowAnonymous] // Students can upload submissions
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubmissionDto>> UploadSubmission([FromForm] UploadSubmissionRequest request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("No file uploaded");
        }

        if (request.File.Length > Constants.MaxFileSize)
        {
            return BadRequest("File size exceeds maximum allowed size");
        }

        var uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads", request.ExamId.ToString(), request.StudentId);
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        var submission = new Submission
        {
            StudentId = request.StudentId,
            StudentName = request.StudentName,
            ExamId = request.ExamId,
            Status = "Submitted",
            CreatedAt = DateTime.UtcNow
        };

        _context.Submissions.Add(submission);
        await _context.SaveChangesAsync();

        var submissionPath = Path.Combine(uploadsPath, submission.Id.ToString());
        Directory.CreateDirectory(submissionPath);

        var filePath = Path.Combine(submissionPath, request.File.FileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        // Extract RAR/ZIP file
        var extractionPath = Path.Combine(submissionPath, "extracted");
        using (var fileStream = new FileStream(filePath, FileMode.Open))
        {
            await _fileService.ExtractRarAsync(fileStream, extractionPath);
        }

        // Process extracted files
        var files = await _fileService.ProcessFilesAsync(extractionPath);
        foreach (var fileInfo in files)
        {
            var submissionFile = new SubmissionFile
            {
                SubmissionId = submission.Id,
                FileName = fileInfo.Name,
                FileType = _fileService.ClassifyFileType(fileInfo.Name),
                FilePath = fileInfo.FullName,
                FileSize = fileInfo.Length,
                CreatedAt = DateTime.UtcNow
            };
            _context.SubmissionFiles.Add(submissionFile);
        }

        await _context.SaveChangesAsync();

        // Load submission with files
        var result = await _context.Submissions
            .Include(s => s.Files)
            .Include(s => s.Grades)
            .FirstOrDefaultAsync(s => s.Id == submission.Id);

        return Ok(new SubmissionDto
        {
            Id = result!.Id,
            StudentId = result.StudentId,
            StudentName = result.StudentName,
            ExamId = result.ExamId,
            Status = result.Status,
            TotalScore = result.TotalScore,
            CreatedAt = result.CreatedAt,
            Files = result.Files.Select(f => new SubmissionFileDto
            {
                Id = f.Id,
                SubmissionId = f.SubmissionId,
                FileName = f.FileName,
                FileType = f.FileType,
                FilePath = f.FilePath,
                FileSize = f.FileSize,
                CreatedAt = f.CreatedAt
            }).ToList(),
            Grades = result.Grades.Select(g => new GradeDto
            {
                Id = g.Id,
                SubmissionId = g.SubmissionId,
                ExaminerId = g.ExaminerId,
                Scores = g.Scores,
                Comment = g.Comment,
                TotalScore = g.TotalScore,
                CreatedAt = g.CreatedAt
            }).ToList()
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Manager,Moderator,Examiner")]
    [ProducesResponseType(typeof(List<SubmissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SubmissionDto>>> GetSubmissions([FromQuery] int? examId = null)
    {
        try
        {
            var userRole = User.GetRole();
            var userId = User.GetUserId();
            
            var query = _context.Submissions
                .Include(s => s.Files)
                .Include(s => s.Grades)
                .AsQueryable();

            // Filter by examId if provided
            if (examId.HasValue)
            {
                query = query.Where(s => s.ExamId == examId.Value);
            }

            // If user is Examiner, only show submissions assigned to them
            if (userRole == "Examiner" && !string.IsNullOrEmpty(userId) && int.TryParse(userId, out var userIdInt))
            {
                try
                {
                    // Get examiner ID from ExaminerService by user ID
                    var examinerClient = _httpClientFactory.CreateClient("ExaminerService");
                    var examinerResponse = await examinerClient.GetAsync("/api/examiners");
                    if (examinerResponse.IsSuccessStatusCode)
                    {
                        var examinerContent = await examinerResponse.Content.ReadAsStringAsync();
                        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var examiners = JsonSerializer.Deserialize<List<ExaminerDto>>(examinerContent, jsonOptions);
                        
                        var examiner = examiners?.FirstOrDefault(e => e.UserId == userIdInt);
                        if (examiner != null)
                        {
                            // Get assignments for this examiner
                            var assignmentResponse = await examinerClient.GetAsync($"/api/examiners/assignments?examinerId={examiner.Id}");
                            if (assignmentResponse.IsSuccessStatusCode)
                            {
                                var assignmentContent = await assignmentResponse.Content.ReadAsStringAsync();
                                var assignments = JsonSerializer.Deserialize<List<AssignmentDto>>(assignmentContent, jsonOptions);
                                
                                if (assignments != null && assignments.Any())
                                {
                                    var assignedSubmissionIds = assignments.Select(a => a.SubmissionId).ToList();
                                    query = query.Where(s => assignedSubmissionIds.Contains(s.Id));
                                }
                                else
                                {
                                    // No assignments, return empty list
                                    return Ok(new List<SubmissionDto>());
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting examiner assignments. Will show all submissions.");
                    // If error, don't filter - show all (fallback behavior)
                }
            }

            var submissions = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var result = submissions.Select(submission => new SubmissionDto
            {
                Id = submission.Id,
                StudentId = submission.StudentId,
                StudentName = submission.StudentName,
                ExamId = submission.ExamId,
                Status = submission.Status,
                TotalScore = submission.TotalScore,
                CreatedAt = submission.CreatedAt,
                Files = submission.Files.Select(f => new SubmissionFileDto
                {
                    Id = f.Id,
                    SubmissionId = f.SubmissionId,
                    FileName = f.FileName,
                    FileType = f.FileType,
                    FilePath = f.FilePath,
                    FileSize = f.FileSize,
                    CreatedAt = f.CreatedAt
                }).ToList(),
                Grades = submission.Grades.Select(g => new GradeDto
                {
                    Id = g.Id,
                    SubmissionId = g.SubmissionId,
                    ExaminerId = g.ExaminerId,
                    Scores = g.Scores,
                    Comment = g.Comment,
                    TotalScore = g.TotalScore,
                    CreatedAt = g.CreatedAt
                }).ToList()
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving submissions. ExamId: {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while retrieving submissions", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Manager,Moderator,Examiner")]
    public async Task<ActionResult<SubmissionDto>> GetSubmission(int id)
    {
        var submission = await _context.Submissions
            .Include(s => s.Files)
            .Include(s => s.Grades)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (submission == null)
        {
            return NotFound();
        }

        return Ok(new SubmissionDto
        {
            Id = submission.Id,
            StudentId = submission.StudentId,
            StudentName = submission.StudentName,
            ExamId = submission.ExamId,
            Status = submission.Status,
            TotalScore = submission.TotalScore,
            CreatedAt = submission.CreatedAt,
            Files = submission.Files.Select(f => new SubmissionFileDto
            {
                Id = f.Id,
                SubmissionId = f.SubmissionId,
                FileName = f.FileName,
                FileType = f.FileType,
                FilePath = f.FilePath,
                FileSize = f.FileSize,
                CreatedAt = f.CreatedAt
            }).ToList(),
            Grades = submission.Grades.Select(g => new GradeDto
            {
                Id = g.Id,
                SubmissionId = g.SubmissionId,
                ExaminerId = g.ExaminerId,
                Scores = g.Scores,
                Comment = g.Comment,
                TotalScore = g.TotalScore,
                CreatedAt = g.CreatedAt
            }).ToList()
        });
    }

    [HttpGet("{submissionId}/files/{fileId}/download")]
    [Authorize(Roles = "Admin,Manager,Moderator,Examiner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFile(int submissionId, int fileId)
    {
        try
        {
            var submission = await _context.Submissions
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return NotFound(new { message = "Submission not found" });
            }

            var file = submission.Files.FirstOrDefault(f => f.Id == fileId);
            if (file == null)
            {
                return NotFound(new { message = "File not found" });
            }

            if (!System.IO.File.Exists(file.FilePath))
            {
                return NotFound(new { message = "File does not exist on server" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
            var contentType = GetContentType(file.FileName);

            return File(fileBytes, contentType, file.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId} from submission {SubmissionId}", fileId, submissionId);
            return StatusCode(500, new { message = "An error occurred while downloading the file", error = ex.Message });
        }
    }

    [HttpGet("{submissionId}/files/{fileId}/view")]
    [Authorize(Roles = "Admin,Manager,Moderator,Examiner")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ViewFile(int submissionId, int fileId)
    {
        try
        {
            var submission = await _context.Submissions
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null)
            {
                return NotFound(new { message = "Submission not found" });
            }

            var file = submission.Files.FirstOrDefault(f => f.Id == fileId);
            if (file == null)
            {
                return NotFound(new { message = "File not found" });
            }

            if (!System.IO.File.Exists(file.FilePath))
            {
                return NotFound(new { message = "File does not exist on server" });
            }

            var fileContent = await System.IO.File.ReadAllTextAsync(file.FilePath);
            var contentType = GetContentType(file.FileName);

            return Content(fileContent, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing file {FileId} from submission {SubmissionId}", fileId, submissionId);
            return StatusCode(500, new { message = "An error occurred while viewing the file", error = ex.Message });
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".cs" => "text/plain",
            ".java" => "text/plain",
            ".cpp" => "text/plain",
            ".c" => "text/plain",
            ".py" => "text/plain",
            ".js" => "text/javascript",
            ".ts" => "text/typescript",
            ".html" => "text/html",
            ".css" => "text/css",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            _ => "application/octet-stream"
        };
    }

    [HttpPost("{id}/grade")]
    [Authorize(Roles = "Examiner")] // Only Examiner can grade, not Admin
    [ProducesResponseType(typeof(GradeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GradeDto>> GradeSubmission(int id, GradeSubmissionRequest request)
    {
        var submission = await _context.Submissions.FindAsync(id);
        if (submission == null)
        {
            return NotFound();
        }

        var grade = new Grade
        {
            SubmissionId = id,
            ExaminerId = request.ExaminerId,
            Scores = request.Scores,
            Comment = request.Comment,
            TotalScore = request.TotalScore,
            CreatedAt = DateTime.UtcNow
        };

        _context.Grades.Add(grade);
        
        submission.TotalScore = request.TotalScore;
        submission.Status = "Graded";
        submission.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new GradeDto
        {
            Id = grade.Id,
            SubmissionId = grade.SubmissionId,
            ExaminerId = grade.ExaminerId,
            Scores = grade.Scores,
            Comment = grade.Comment,
            TotalScore = grade.TotalScore,
            CreatedAt = grade.CreatedAt
        });
    }
}


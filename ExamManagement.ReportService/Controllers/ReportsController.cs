using ExamManagement.Common;
using ExamManagement.Contracts.Report;
using ExamManagement.Contracts.Submission;
using ExamManagement.Contracts.Violation;
using ExamManagement.Models.Report;
using ExamManagement.ReportService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text.Json;

namespace ExamManagement.ReportService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ReportDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ReportsController> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ReportsController(
        ReportDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<ReportsController> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("exam")]
    [Authorize(Roles = "Admin,Manager,Moderator")]
    [ProducesResponseType(typeof(List<ExamReportDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExamReportDto>>> GetExamReports([FromQuery] int examId)
    {
        try
        {
            // Get submissions from SubmissionService
            var submissionClient = _httpClientFactory.CreateClient("SubmissionService");
            var submissionResponse = await submissionClient.GetAsync($"/api/submissions?examId={examId}");
            
            if (!submissionResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get submissions from SubmissionService. Status: {StatusCode}", submissionResponse.StatusCode);
                return StatusCode((int)submissionResponse.StatusCode, new { message = "Failed to retrieve submissions" });
            }

            var submissionContent = await submissionResponse.Content.ReadAsStringAsync();
            var submissions = JsonSerializer.Deserialize<List<SubmissionDto>>(submissionContent, _jsonOptions) ?? new List<SubmissionDto>();

            // Get violations for each submission
            var violationClient = _httpClientFactory.CreateClient("ViolationService");
            var reportData = new List<ExamReportDto>();

            foreach (var submission in submissions)
            {
                var violationResponse = await violationClient.GetAsync($"/api/violations/{submission.Id}");
                var violations = new List<ViolationDto>();
                
                if (violationResponse.IsSuccessStatusCode)
                {
                    var violationContent = await violationResponse.Content.ReadAsStringAsync();
                    violations = JsonSerializer.Deserialize<List<ViolationDto>>(violationContent, _jsonOptions) ?? new List<ViolationDto>();
                }

                reportData.Add(new ExamReportDto
                {
                    SubmissionId = submission.Id,
                    StudentId = submission.StudentId,
                    StudentName = submission.StudentName,
                    ExamId = submission.ExamId,
                    Status = submission.Status,
                    TotalScore = submission.TotalScore,
                    CreatedAt = submission.CreatedAt,
                    ViolationCount = violations.Count,
                    HasViolations = violations.Any(v => !v.IsResolved)
                });
            }

            return Ok(reportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam reports for examId: {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while retrieving exam reports", error = ex.Message });
        }
    }

    [HttpGet("exam/export")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportExamReport([FromQuery] int examId)
    {
        try
        {
            // Set EPPlus license (required for EPPlus 8.2.1+)
            // Must be set before creating ExcelPackage instance
            // Using NonCommercial for educational/non-profit use
            ExcelPackage.License.SetNonCommercialOrganization("Exam Management System");
            
            // Get submissions from SubmissionService
            var submissionClient = _httpClientFactory.CreateClient("SubmissionService");
            var submissionResponse = await submissionClient.GetAsync($"/api/submissions?examId={examId}");
            
            if (!submissionResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get submissions from SubmissionService. Status: {StatusCode}", submissionResponse.StatusCode);
                return StatusCode((int)submissionResponse.StatusCode, new { message = "Failed to retrieve submissions" });
            }

            var submissionContent = await submissionResponse.Content.ReadAsStringAsync();
            var submissions = JsonSerializer.Deserialize<List<SubmissionDto>>(submissionContent, _jsonOptions) ?? new List<SubmissionDto>();

            if (!submissions.Any())
            {
                return BadRequest(new { message = "No submissions found for this exam" });
            }

            // Get violations for each submission
            var violationClient = _httpClientFactory.CreateClient("ViolationService");
            var submissionViolations = new Dictionary<int, List<ViolationDto>>();

            foreach (var submission in submissions)
            {
                var violationResponse = await violationClient.GetAsync($"/api/violations/{submission.Id}");
                if (violationResponse.IsSuccessStatusCode)
                {
                    var violationContent = await violationResponse.Content.ReadAsStringAsync();
                    var violations = JsonSerializer.Deserialize<List<ViolationDto>>(violationContent, _jsonOptions) ?? new List<ViolationDto>();
                    submissionViolations[submission.Id] = violations;
                }
            }

            // Create Excel file
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Exam Report");

            // Header
            worksheet.Cells[1, 1].Value = "STT";
            worksheet.Cells[1, 2].Value = "Mã SV";
            worksheet.Cells[1, 3].Value = "Tên SV";
            worksheet.Cells[1, 4].Value = "Điểm";
            worksheet.Cells[1, 5].Value = "Trạng thái";
            worksheet.Cells[1, 6].Value = "Số vi phạm";
            worksheet.Cells[1, 7].Value = "Có vi phạm";
            worksheet.Cells[1, 8].Value = "Ngày nộp";

            // Style header
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Data rows
            var row = 2;
            var stt = 1;
            foreach (var submission in submissions.OrderBy(s => s.StudentId))
            {
                var violations = submissionViolations.GetValueOrDefault(submission.Id, new List<ViolationDto>());
                var unresolvedViolations = violations.Where(v => !v.IsResolved).ToList();

                worksheet.Cells[row, 1].Value = stt++;
                worksheet.Cells[row, 2].Value = submission.StudentId;
                worksheet.Cells[row, 3].Value = submission.StudentName;
                worksheet.Cells[row, 4].Value = submission.TotalScore ?? 0;
                worksheet.Cells[row, 5].Value = submission.Status;
                worksheet.Cells[row, 6].Value = violations.Count;
                worksheet.Cells[row, 7].Value = unresolvedViolations.Any() ? "Có" : "Không";
                worksheet.Cells[row, 8].Value = submission.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                // Highlight rows with violations
                if (unresolvedViolations.Any())
                {
                    using (var range = worksheet.Cells[row, 1, row, 8])
                    {
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                    }
                }

                row++;
            }

            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();

            // Add summary sheet
            var summarySheet = package.Workbook.Worksheets.Add("Tổng hợp");
            summarySheet.Cells[1, 1].Value = "Tổng số bài nộp:";
            summarySheet.Cells[1, 2].Value = submissions.Count;
            summarySheet.Cells[2, 1].Value = "Số bài có vi phạm:";
            summarySheet.Cells[2, 2].Value = submissions.Count(s => submissionViolations.GetValueOrDefault(s.Id, new List<ViolationDto>()).Any(v => !v.IsResolved));
            summarySheet.Cells[3, 1].Value = "Số bài đã chấm:";
            summarySheet.Cells[3, 2].Value = submissions.Count(s => s.Status == "Graded");
            summarySheet.Cells[4, 1].Value = "Điểm trung bình:";
            summarySheet.Cells[4, 2].Value = submissions.Where(s => s.TotalScore.HasValue).Any() 
                ? submissions.Where(s => s.TotalScore.HasValue).Average(s => s.TotalScore!.Value).ToString("F2")
                : "N/A";

            summarySheet.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                $"ExamReport_{examId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting exam report for examId: {ExamId}", examId);
            return StatusCode(500, new { message = "An error occurred while exporting the report", error = ex.Message });
        }
    }
}


using ExamManagement.Common;
using ExamManagement.Contracts.Submission;
using ExamManagement.Contracts.Violation;
using ExamManagement.Models.Violation;
using ExamManagement.ViolationService.Data;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExamManagement.ViolationService.Services;

public class ViolationService : IViolationService
{
    private readonly ViolationDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ViolationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ViolationService(
        ViolationDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<ViolationService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<Violation>> CheckViolationsAsync(int submissionId, int examId, List<string> fileNames, List<string> filePaths)
    {
        var violations = new List<Violation>();

        // Get actual submission details from SubmissionService to get real file paths
        var httpClient = _httpClientFactory.CreateClient("SubmissionService");
        SubmissionDto? currentSubmission = null;
        try
        {
            var submissionResponse = await httpClient.GetAsync($"/api/submissions/{submissionId}");
            if (submissionResponse.IsSuccessStatusCode)
            {
                var submissionContent = await submissionResponse.Content.ReadAsStringAsync();
                currentSubmission = JsonSerializer.Deserialize<SubmissionDto>(submissionContent, _jsonOptions);
                
                if (currentSubmission != null && currentSubmission.Files != null && currentSubmission.Files.Any())
                {
                    // Use actual file paths from SubmissionService instead of request
                    fileNames = currentSubmission.Files.Select(f => f.FileName).ToList();
                    filePaths = currentSubmission.Files.Select(f => f.FilePath).ToList();
                    _logger.LogInformation("Using {FileCount} files from SubmissionService for submission {SubmissionId}", 
                        fileNames.Count, submissionId);
                }
            }
            else
            {
                _logger.LogWarning("Could not get submission details from SubmissionService. Status: {StatusCode}, SubmissionId: {SubmissionId}", 
                    submissionResponse.StatusCode, submissionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting submission details from SubmissionService. Will use provided fileNames/filePaths. SubmissionId: {SubmissionId}", submissionId);
        }

        // Validate inputs
        if (fileNames == null || filePaths == null || !fileNames.Any() || !filePaths.Any())
        {
            _logger.LogWarning("FileNames or FilePaths is null or empty. SubmissionId: {SubmissionId}", submissionId);
            return violations; // Return empty list instead of throwing
        }

        // Delete existing violations for this submission before creating new ones
        try
        {
            var existingViolations = await _context.Violations
                .Where(v => v.SubmissionId == submissionId)
                .ToListAsync();
            
            if (existingViolations.Any())
            {
                _context.Violations.RemoveRange(existingViolations);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted {Count} existing violations for submission {SubmissionId}", existingViolations.Count, submissionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting existing violations. SubmissionId: {SubmissionId}", submissionId);
            // Continue with creating new violations even if delete fails
        }

        if (fileNames.Count != filePaths.Count)
        {
            _logger.LogWarning("FileNames and FilePaths count mismatch. SubmissionId: {SubmissionId}, FileNames: {FileNamesCount}, FilePaths: {FilePathsCount}", 
                submissionId, fileNames.Count, filePaths.Count);
            // Continue with available data
        }

        // Check file name violations
        try
        {
            foreach (var fileName in fileNames)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    continue;

                if (!IsValidFileName(fileName))
                {
                    violations.Add(new Violation
                    {
                        SubmissionId = submissionId,
                        Type = "FileName",
                        Description = $"Invalid file name format: {fileName}",
                        Severity = "Medium",
                        CreatedAt = DateTime.Now,
                        IsResolved = false
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking file name violations. SubmissionId: {SubmissionId}", submissionId);
        }

        // Check keyword violations
        try
        {
            // Check if ViolationKeywords table exists
            if (await _context.Database.CanConnectAsync())
            {
                var activeKeywords = await _context.ViolationKeywords
                    .Where(k => k.IsActive)
                    .ToListAsync();

                if (!activeKeywords.Any())
                {
                    _logger.LogInformation("No active keywords found for violation checking. SubmissionId: {SubmissionId}", submissionId);
                }
                else
                {
                    _logger.LogInformation("Checking {KeywordCount} active keywords for submission {SubmissionId}", activeKeywords.Count, submissionId);
                    
                    // Check in file names
                    foreach (var fileName in fileNames)
                    {
                        if (string.IsNullOrWhiteSpace(fileName))
                            continue;

                        foreach (var keyword in activeKeywords)
                        {
                            if (string.IsNullOrWhiteSpace(keyword.Keyword))
                                continue;

                            if (fileName.Contains(keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                violations.Add(new Violation
                                {
                                    SubmissionId = submissionId,
                                    Type = "KeywordViolation",
                                    Description = $"File name contains prohibited keyword '{keyword.Keyword}': {fileName}. {keyword.Description}",
                                    Severity = keyword.Severity,
                                    CreatedAt = DateTime.Now,
                                    IsResolved = false
                                });
                                _logger.LogInformation("Keyword violation found: '{Keyword}' in file '{FileName}' for submission {SubmissionId}", 
                                    keyword.Keyword, fileName, submissionId);
                            }
                        }
                    }

                    // Check in file paths and file content
                    foreach (var filePath in filePaths)
                    {
                        if (string.IsNullOrWhiteSpace(filePath))
                            continue;

                        var fileNameFromPath = Path.GetFileName(filePath);
                        bool foundInFileName = false;

                        // Check in file name and path
                        foreach (var keyword in activeKeywords)
                        {
                            if (string.IsNullOrWhiteSpace(keyword.Keyword))
                                continue;

                            // Check in file name and full path
                            if (fileNameFromPath.Contains(keyword.Keyword, StringComparison.OrdinalIgnoreCase) ||
                                filePath.Contains(keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                violations.Add(new Violation
                                {
                                    SubmissionId = submissionId,
                                    Type = "KeywordViolation",
                                    Description = $"File path contains prohibited keyword '{keyword.Keyword}': {filePath}. {keyword.Description}",
                                    Severity = keyword.Severity,
                                    CreatedAt = DateTime.Now,
                                    IsResolved = false
                                });
                                _logger.LogInformation("Keyword violation found: '{Keyword}' in path '{FilePath}' for submission {SubmissionId}", 
                                    keyword.Keyword, filePath, submissionId);
                                break; // Only report once per file path
                            }
                        }

                        // Check in file content (only for text files, even if found in file name)
                        if (IsTextFile(filePath))
                        {
                            try
                            {
                                if (System.IO.File.Exists(filePath))
                                {
                                    // Read file content (limit to 10MB to avoid memory issues)
                                    var fileInfo = new FileInfo(filePath);
                                    if (fileInfo.Length > 10 * 1024 * 1024) // 10MB limit
                                    {
                                        _logger.LogWarning("File too large to check content: {FilePath} ({Size} bytes)", filePath, fileInfo.Length);
                                        continue;
                                    }

                                    string fileContent;
                                    try
                                    {
                                        // Try UTF-8 first
                                        fileContent = await System.IO.File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);
                                    }
                                    catch
                                    {
                                        // Fallback to default encoding
                                        fileContent = await System.IO.File.ReadAllTextAsync(filePath);
                                    }

                                    _logger.LogInformation("Checking keywords in file content: {FilePath} ({Size} bytes, {ContentLength} chars)", 
                                        filePath, fileInfo.Length, fileContent.Length);

                                    // Check keywords in file content
                                    foreach (var keyword in activeKeywords)
                                    {
                                        if (string.IsNullOrWhiteSpace(keyword.Keyword))
                                            continue;

                                        if (fileContent.Contains(keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                                        {
                                            violations.Add(new Violation
                                            {
                                                SubmissionId = submissionId,
                                                Type = "KeywordViolation",
                                                Description = $"File content contains prohibited keyword '{keyword.Keyword}': {Path.GetFileName(filePath)}. {keyword.Description}",
                                                Severity = keyword.Severity,
                                                CreatedAt = DateTime.Now,
                                                IsResolved = false
                                            });
                                            _logger.LogInformation("Keyword violation found: '{Keyword}' in content of file '{FilePath}' for submission {SubmissionId}", 
                                                keyword.Keyword, filePath, submissionId);
                                            break; // Only report once per file
                                        }
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("File does not exist for keyword checking: {FilePath}", filePath);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Could not read file content for keyword checking: {FilePath}", filePath);
                                // Continue with other files
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Skipping non-text file for content checking: {FilePath}", filePath);
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("Cannot connect to database for keyword checking. SubmissionId: {SubmissionId}", submissionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking keyword violations. SubmissionId: {SubmissionId}", submissionId);
            // Continue with other checks even if keyword check fails
        }

        // Check for file name matches with other submissions in the same exam
        try
        {
            var response = await httpClient.GetAsync($"/api/submissions?examId={examId}");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var otherSubmissions = JsonSerializer.Deserialize<List<SubmissionDto>>(content, _jsonOptions);

                if (otherSubmissions != null && otherSubmissions.Any())
                {
                    // Get file names from current submission (normalize for comparison, exclude build artifacts)
                    var currentFileNames = fileNames
                        .Where(f => !string.IsNullOrWhiteSpace(f) && !IsBuildArtifact(f))
                        .Select(f => Path.GetFileName(f).ToLowerInvariant())
                        .Where(f => !string.IsNullOrWhiteSpace(f))
                        .OrderBy(f => f)
                        .ToList();

                    if (currentFileNames.Any())
                    {
                        // Compare with other submissions
                        foreach (var otherSubmission in otherSubmissions.Where(s => s.Id != submissionId))
                        {
                            if (otherSubmission?.Files != null && otherSubmission.Files.Any())
                            {
                                var otherFileNames = otherSubmission.Files
                                    .Where(f => f != null && !string.IsNullOrWhiteSpace(f.FileName) && !IsBuildArtifact(f.FileName))
                                    .Select(f => f.FileName.ToLowerInvariant())
                                    .OrderBy(f => f)
                                    .ToList();

                                if (otherFileNames.Any())
                                {
                                    // Check if file name lists are identical
                                    if (currentFileNames.SequenceEqual(otherFileNames))
                                    {
                                        violations.Add(new Violation
                                        {
                                            SubmissionId = submissionId,
                                            Type = "FileNameMatch",
                                            Description = $"File name list matches submission #{otherSubmission.Id} (Student: {otherSubmission.StudentName ?? "Unknown"}, ID: {otherSubmission.StudentId ?? "Unknown"}). Possible plagiarism or collaboration.",
                                            Severity = "Critical",
                                            CreatedAt = DateTime.Now,
                                            IsResolved = false
                                        });
                                        break; // Only report once
                                    }

                                    // Check for partial matches (more than 50% of files match)
                                    var matchingFiles = currentFileNames.Intersect(otherFileNames).ToList();
                                    if (matchingFiles.Any())
                                    {
                                        var matchPercentage = (double)matchingFiles.Count / Math.Max(currentFileNames.Count, otherFileNames.Count) * 100;
                                        
                                        if (matchPercentage > 50 && matchingFiles.Count >= 3)
                                        {
                                            violations.Add(new Violation
                                            {
                                                SubmissionId = submissionId,
                                                Type = "FileNameMatch",
                                                Description = $"High similarity ({matchPercentage:F1}%) with submission #{otherSubmission.Id} (Student: {otherSubmission.StudentName ?? "Unknown"}, ID: {otherSubmission.StudentId ?? "Unknown"}). Matching files: {string.Join(", ", matchingFiles)}",
                                                Severity = "High",
                                                CreatedAt = DateTime.Now,
                                                IsResolved = false
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not check file name matches with other submissions. ExamId: {ExamId}, SubmissionId: {SubmissionId}", examId, submissionId);
            // Continue with other checks even if this fails
        }

        // Save violations to database
        try
        {
            if (violations.Any())
            {
                _context.Violations.AddRange(violations);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving violations to database. SubmissionId: {SubmissionId}", submissionId);
            // Return violations even if save fails
        }

        return violations;
    }

    public async Task<List<ViolationDto>> GetViolationsAsync(int submissionId)
    {
        var violations = await _context.Violations
            .Where(v => v.SubmissionId == submissionId)
            .Select(v => new ViolationDto
            {
                Id = v.Id,
                SubmissionId = v.SubmissionId,
                Type = v.Type,
                Description = v.Description,
                Severity = v.Severity,
                CreatedAt = v.CreatedAt,
                IsResolved = v.IsResolved
            })
            .ToListAsync();

        return violations;
    }

    private bool IsValidFileName(string fileName)
    {
        // Check if file name follows the pattern: StudentId_StudentName_FileName
        // This is a simplified check - adjust based on your requirements
        var invalidChars = Path.GetInvalidFileNameChars();
        if (fileName.IndexOfAny(invalidChars) >= 0)
        {
            return false;
        }

        // Add more validation rules as needed
        return true;
    }

    private bool IsCodeFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        var codeExtensions = new[] { ".cs", ".java", ".cpp", ".c", ".py", ".js", ".ts", ".html", ".css" };
        return codeExtensions.Contains(extension);
    }

    private bool IsTextFile(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        var textExtensions = new[] 
        { 
            ".cs", ".java", ".cpp", ".c", ".h", ".hpp", 
            ".py", ".js", ".ts", ".jsx", ".tsx",
            ".html", ".css", ".scss", ".sass",
            ".json", ".xml", ".yaml", ".yml",
            ".txt", ".md", ".markdown",
            ".sql", ".sh", ".bat", ".ps1",
            ".config", ".csproj", ".sln",
            ".vue", ".jsx", ".tsx"
        };
        return textExtensions.Contains(extension);
    }

    private bool IsBuildArtifact(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var directory = Path.GetDirectoryName(filePath) ?? "";
        
        // Check if file is in obj or bin directories
        if (directory.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase) ||
            directory.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
            directory.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) ||
            directory.Contains("/bin/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for common build artifact file patterns
        var buildArtifactPatterns = new[]
        {
            ".NETCoreApp",
            "AssemblyAttributes",
            "AssemblyInfo",
            ".AssemblyAttributes",
            ".g.cs",
            ".g.i.cs",
            ".Designer.cs",
            "TemporaryGeneratedFile",
            "GlobalUsings.g.cs"
        };

        foreach (var pattern in buildArtifactPatterns)
        {
            if (fileName.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}


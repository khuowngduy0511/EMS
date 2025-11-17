using ExamManagement.Contracts.Violation;
using ExamManagement.Models.Violation;

namespace ExamManagement.ViolationService.Services;

public interface IViolationService
{
    Task<List<Violation>> CheckViolationsAsync(int submissionId, int examId, List<string> fileNames, List<string> filePaths);
    Task<List<ViolationDto>> GetViolationsAsync(int submissionId);
}


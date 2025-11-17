namespace ExamManagement.SubmissionService.Services;

public interface IFileService
{
    Task<string> ExtractRarAsync(Stream rarStream, string extractionPath);
    Task<List<FileInfo>> ProcessFilesAsync(string directory);
    string ClassifyFileType(string fileName);
}


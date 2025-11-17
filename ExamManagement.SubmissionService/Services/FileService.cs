using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using System.IO.Compression;

namespace ExamManagement.SubmissionService.Services;

public class FileService : IFileService
{
    public async Task<string> ExtractRarAsync(Stream rarStream, string extractionPath)
    {
        if (!Directory.Exists(extractionPath))
        {
            Directory.CreateDirectory(extractionPath);
        }

        rarStream.Position = 0;
        
        try
        {
            using var archive = ArchiveFactory.Open(rarStream);
            foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
            {
                var entryKey = entry.Key;
                if (string.IsNullOrEmpty(entryKey))
                    continue;
                    
                var destinationPath = Path.Combine(extractionPath, entryKey);
                var directory = Path.GetDirectoryName(destinationPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                using var entryStream = entry.OpenEntryStream();
                using var fileStream = File.Create(destinationPath);
                await entryStream.CopyToAsync(fileStream);
            }
        }
        catch
        {
            // Fallback to ZIP if RAR extraction fails
            rarStream.Position = 0;
            using var archive = new ZipArchive(rarStream, ZipArchiveMode.Read);
            foreach (var entry in archive.Entries)
            {
                var entryFullName = entry.FullName;
                if (string.IsNullOrEmpty(entryFullName))
                    continue;
                    
                var destinationPath = Path.Combine(extractionPath, entryFullName);
                var directory = Path.GetDirectoryName(destinationPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                if (!string.IsNullOrEmpty(entry.Name))
                {
                    entry.ExtractToFile(destinationPath, true);
                }
            }
        }

        return extractionPath;
    }

    public async Task<List<FileInfo>> ProcessFilesAsync(string directory)
    {
        var files = new List<FileInfo>();
        var directoryInfo = new DirectoryInfo(directory);
        
        foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
        {
            files.Add(file);
        }
        
        return await Task.FromResult(files);
    }

    public string ClassifyFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        
        return extension switch
        {
            ".doc" or ".docx" => "Word",
            ".cs" or ".java" or ".cpp" or ".c" or ".py" or ".js" or ".ts" or ".html" or ".css" => "Code",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "Image",
            _ => "Other"
        };
    }
}


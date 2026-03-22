using System.IO.Compression;
using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Server-side implementation of ILogDownloadService for creating log archive streams.
/// Gathers logs from package logs directory, debug log file, and application logs directory.
/// </summary>
public class LogDownloadService(ILogger<LogDownloadService> logger) : Data.Services.ILogDownloadService
{
    private readonly ILogger<LogDownloadService> _logger = logger;

    public async Task<Stream> CreateLogZipStreamAsync()
    {
        var baseDirectory = AppContext.BaseDirectory;
        _logger.LogDebug("Creating log archive stream (BaseDirectory: {BaseDirectory})", baseDirectory);

        var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add package logs directory if it exists
            await TryAddDirectoryToArchiveAsync(archive, LogConstants.PackageLogDirectoryPath, "package-logs", "Package logs", baseDirectory);

            // Add debug log file if it exists
            if (File.Exists(LogConstants.DebugLogFilePath))
            {
                await AddFileToArchiveAsync(archive, LogConstants.DebugLogFilePath, "debug-logs/adwh-debug.log");
                _logger.LogDebug("Added debug log file: {DebugLogFilePath}", LogConstants.DebugLogFilePath);
            }
            else
            {
                _logger.LogWarning("Debug log file not found at path: {DebugLogFilePath}", LogConstants.DebugLogFilePath);
            }

            // Add application logs directory if it exists
            var logsPath = Path.Combine(baseDirectory, LogConstants.LogsDirectoryName);
            await TryAddDirectoryToArchiveAsync(archive, logsPath, "application-logs", "Application logs", baseDirectory);
        }

        memoryStream.Position = 0;

        _logger.LogInformation("Created log archive stream with size {Size} bytes", memoryStream.Length);

        return memoryStream;
    }

    private async Task TryAddDirectoryToArchiveAsync(ZipArchive archive, string directoryPath, string entryPrefix, string logName, string baseDirectory)
    {
        if (Directory.Exists(directoryPath))
        {
            await AddDirectoryToArchiveAsync(archive, directoryPath, entryPrefix);
            _logger.LogDebug("Added {LogName} from directory: {DirectoryPath} (BaseDirectory: {BaseDirectory})", logName, directoryPath, baseDirectory);
        }
        else
        {
            _logger.LogWarning("{LogName} directory not found at path: {DirectoryPath} (BaseDirectory: {BaseDirectory})", logName, directoryPath, baseDirectory);
        }
    }

    private async Task AddDirectoryToArchiveAsync(ZipArchive archive, string directoryPath, string entryPrefix)
    {
        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(directoryPath, file);
            var entryName = Path.Combine(entryPrefix, relativePath).Replace(Path.DirectorySeparatorChar, '/');

            await AddFileToArchiveAsync(archive, file, entryName);
        }
    }

    private async Task AddFileToArchiveAsync(ZipArchive archive, string filePath, string entryName)
    {
        var entry = archive.CreateEntry(entryName);

        using var entryStream = entry.Open();
        using var fileStream = File.OpenRead(filePath);

        await fileStream.CopyToAsync(entryStream);

        _logger.LogDebug("Added file to archive: {EntryName}", entryName);
    }
}

using System.IO.Compression;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Logging;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Server-side implementation of ILogDownloadService for creating log archive streams.
/// Gathers logs from package logs directory, debug log file, and application logs directory.
/// </summary>
public class LogDownloadService(ILogger<LogDownloadService> logger) : Data.Contracts.ILogDownloadService
{
    public async Task<Stream> CreateLogZipStreamAsync()
    {
        var baseDirectory = AppContext.BaseDirectory;
        logger.CreatingLogArchiveStream(baseDirectory);

        var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add package logs directory if it exists
            await TryAddDirectoryToArchiveAsync(archive, LogConstants.PackageLogDirectoryPath, "package-logs", "Package logs", baseDirectory);

            // Add debug log file if it exists
            if (File.Exists(LogConstants.DebugLogFilePath))
            {
                await AddFileToArchiveAsync(archive, LogConstants.DebugLogFilePath, "debug-logs/adwh-debug.log");
                logger.AddedDebugLogFile(LogConstants.DebugLogFilePath);
            }
            else
            {
                logger.DebugLogNotFound(LogConstants.DebugLogFilePath);
            }

            // Add application logs directory if it exists
            var logsPath = Path.Combine(baseDirectory, LogConstants.LogsDirectoryName);
            await TryAddDirectoryToArchiveAsync(archive, logsPath, "application-logs", "Application logs", baseDirectory);
        }

        memoryStream.Position = 0;

        logger.CreatedLogArchiveStream(memoryStream.Length);

        return memoryStream;
    }

    private async Task TryAddDirectoryToArchiveAsync(ZipArchive archive, string directoryPath, string entryPrefix, string logName, string baseDirectory)
    {
        if (Directory.Exists(directoryPath))
        {
            await AddDirectoryToArchiveAsync(archive, directoryPath, entryPrefix);
            logger.AddedAppLog(logName, directoryPath, baseDirectory);
        }
        else
        {
            logger.AppLogDirectoryNotFound(logName, directoryPath, baseDirectory);
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

        logger.AddedFileToArchive(entryName);
    }
}

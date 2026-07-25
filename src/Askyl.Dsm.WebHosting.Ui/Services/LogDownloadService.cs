using System.IO.Compression;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Server-side implementation of ILogDownloadService for creating log archive streams.
/// Gathers logs from package logs directory, debug log file, and application logs directory.
/// </summary>
/// <param name="logger">Logger instance.</param>
/// <param name="fileReader">File system abstraction for reading files and directories.</param>
public class LogDownloadService(ILogger<ILogLogDownloadService> logger, IFileReader fileReader) : Data.Contracts.ILogDownloadService
{
    public async Task<Stream> CreateLogZipStreamAsync(CancellationToken cancellationToken = default)
    {
        var baseDirectory = AppContext.BaseDirectory;
        logger.CreatingLogArchiveStream(baseDirectory);

        var memoryStream = new MemoryStream();

        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            // Add package logs directory if it exists
            await TryAddDirectoryToArchiveAsync(archive, LogConstants.PackageLogDirectoryPath, LogConstants.LogArchivePackagePrefix, LogConstants.LogArchivePackageDisplayName, baseDirectory, cancellationToken);

            // Add debug log file if it exists
            if (fileReader.FileExists(LogConstants.DebugLogFilePath))
            {
                await AddFileToArchiveAsync(archive, LogConstants.DebugLogFilePath, LogConstants.LogArchiveDebugEntryPath, cancellationToken);
                logger.AddedDebugLogFile(LogConstants.DebugLogFilePath);
            }
            else
            {
                logger.DebugLogNotFound(LogConstants.DebugLogFilePath);
            }

            // Add application logs directory if it exists
            var logsPath = Path.Combine(baseDirectory, LogConstants.LogsDirectoryName);
            await TryAddDirectoryToArchiveAsync(archive, logsPath, LogConstants.LogArchiveAppPrefix, LogConstants.LogArchiveAppDisplayName, baseDirectory, cancellationToken);
        }

        memoryStream.Position = 0;

        logger.CreatedLogArchiveStream(memoryStream.Length);

        return memoryStream;
    }

    private async Task TryAddDirectoryToArchiveAsync(ZipArchive archive, string directoryPath, string entryPrefix, string logName, string baseDirectory, CancellationToken cancellationToken)
    {
        if (fileReader.DirectoryExists(directoryPath))
        {
            await AddDirectoryToArchiveAsync(archive, directoryPath, entryPrefix, cancellationToken);
            logger.AddedAppLog(logName, directoryPath, baseDirectory);
        }
        else
        {
            logger.AppLogDirectoryNotFound(logName, directoryPath, baseDirectory);
        }
    }

    private async Task AddDirectoryToArchiveAsync(ZipArchive archive, string directoryPath, string entryPrefix, CancellationToken cancellationToken)
    {
        foreach (var file in fileReader.GetFiles(directoryPath, "*", true))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(directoryPath, file);
            var entryName = Path.Combine(entryPrefix, relativePath).Replace(Path.DirectorySeparatorChar, '/');

            await AddFileToArchiveAsync(archive, file, entryName, cancellationToken);
        }
    }

    private async Task AddFileToArchiveAsync(ZipArchive archive, string filePath, string entryName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entry = archive.CreateEntry(entryName);

        using var entryStream = entry.Open();
        using var fileStream = fileReader.OpenRead(filePath);

        await fileStream.CopyToAsync(entryStream, cancellationToken);

        logger.AddedFileToArchive(entryName);
    }
}

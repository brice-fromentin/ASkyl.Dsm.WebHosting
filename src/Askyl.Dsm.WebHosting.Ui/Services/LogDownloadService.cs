using System.IO.Compression;
using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface ILogDownloadService
{
    Task<(byte[] FileContent, string FileName)> CreateLogZipAsync();
    Task<Stream> CreateLogZipStreamAsync();
}

public class LogDownloadService(IWebHostEnvironment environment, ILogger<LogDownloadService> logger) : ILogDownloadService
{

    #region Fields

    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<LogDownloadService> _logger = logger;

    #endregion

    #region Public Methods

    public async Task<(byte[] FileContent, string FileName)> CreateLogZipAsync()
    {
        var logsPath = Path.Combine(_environment.ContentRootPath, LogConstants.LogsDirectoryName);
        var fileName = $"logs-{DateTime.Now.ToString(LogConstants.ArchiveDateTimeFormat)}{LogConstants.ZipFileExtension}";

        _logger.LogDebug("Creating log archive from directory: {LogsPath}", logsPath);

        if (!Directory.Exists(logsPath))
        {
            _logger.LogWarning("Logs directory not found at path: {LogsPath}", logsPath);
            throw new DirectoryNotFoundException($"Logs directory not found at: {logsPath}");
        }

        using var memoryStream = new MemoryStream();
        
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            await AddDirectoryToArchiveAsync(archive, logsPath, "");
        }

        var fileContent = memoryStream.ToArray();
        
        _logger.LogInformation("Created log archive {FileName} with size {Size} bytes", fileName, fileContent.Length);
        
        return (fileContent, fileName);
    }

    public async Task<Stream> CreateLogZipStreamAsync()
    {
        var logsPath = Path.Combine(_environment.ContentRootPath, LogConstants.LogsDirectoryName);

        _logger.LogDebug("Creating log archive stream from directory: {LogsPath}", logsPath);

        if (!Directory.Exists(logsPath))
        {
            _logger.LogWarning("Logs directory not found at path: {LogsPath}", logsPath);
            throw new DirectoryNotFoundException($"Logs directory not found at: {logsPath}");
        }

        var memoryStream = new MemoryStream();
        
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            await AddDirectoryToArchiveAsync(archive, logsPath, "");
        }

        memoryStream.Position = 0;
        
        _logger.LogInformation("Created log archive stream with size {Size} bytes", memoryStream.Length);
        
        return memoryStream;
    }

    #endregion

    #region Private Methods

    private async Task AddDirectoryToArchiveAsync(ZipArchive archive, string directoryPath, string entryPrefix)
    {
        var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(directoryPath, file);
            var entryName = Path.Combine(entryPrefix, relativePath).Replace(Path.DirectorySeparatorChar, '/');
            
            var entry = archive.CreateEntry(entryName);
            
            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(file);
            
            await fileStream.CopyToAsync(entryStream);
            
            _logger.LogDebug("Added file to archive: {EntryName}", entryName);
        }
    }

    #endregion

}
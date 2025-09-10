using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Ui.Services;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogDownloadController(IWebHostEnvironment environment, ILogger<LogDownloadController> logger, ITemporaryTokenService tokenService) : ControllerBase
{

    #region Fields

    private readonly IWebHostEnvironment _environment = environment;
    private readonly ILogger<LogDownloadController> _logger = logger;
    private readonly ITemporaryTokenService _tokenService = tokenService;

    #endregion

    #region Public Methods

    [HttpGet("logs")]
    public IActionResult DownloadLogs([FromQuery] string token)
    {
        if (!_tokenService.ValidateAndConsumeToken(token))
        {
            _logger.LogWarning("Unauthorized access attempt to download logs - invalid or missing token");
            return Unauthorized("Authentication required");
        }

        var logsPath = Path.Combine(_environment.ContentRootPath, LogConstants.LogsDirectoryName);

        if (!Directory.Exists(logsPath))
        {
            _logger.LogWarning("Logs directory not found at path: {LogsPath}", logsPath);
            return NotFound("Logs directory not found");
        }

        var fileName = $"adwh-logs-{DateTime.Now.ToString(LogConstants.ArchiveDateTimeFormat)}{LogConstants.ZipFileExtension}";

        _logger.LogInformation("Starting streaming download of logs archive: {FileName}", fileName);

        return new FileCallbackResult(LogConstants.ZipMediaType, fileName, async (outputStream, context) =>
        {
            try
            {
                // ZipArchive with direct streaming
                using var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true);
                
                await AddDirectoryToArchiveAsync(archive, logsPath, "");
                
                _logger.LogInformation("Successfully streamed logs archive: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while streaming logs archive: {FileName}", fileName);
                throw;
            }
        });
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
            using var fileStream = System.IO.File.OpenRead(file);
            
            await fileStream.CopyToAsync(entryStream);
            
            _logger.LogDebug("Added file to archive: {EntryName}", entryName);
        }
    }

    #endregion

}

public class FileCallbackResult(string contentType, string fileName, Func<Stream, ActionContext, Task> callback) : IActionResult
{

    #region Fields

    private readonly string _contentType = contentType;
    private readonly string _fileName = fileName;
    private readonly Func<Stream, ActionContext, Task> _callback = callback;

    #endregion

    #region Public Methods

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        
        response.ContentType = _contentType;
        response.Headers.Append("Content-Disposition", $"attachment; filename=\"{_fileName}\"");
        
        await _callback(response.Body, context);
    }

    #endregion

}
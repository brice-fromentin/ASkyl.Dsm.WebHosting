using Microsoft.AspNetCore.Mvc;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Ui.Services;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

[ApiController]
[Route(ApplicationConstants.ApplicationUrlSubPath + "/api/[controller]")]
public class LogDownloadController(ILogger<LogDownloadController> logger, ITemporaryTokenService tokenService, ILogDownloadService logDownloadService) : ControllerBase
{

    #region Fields

    private readonly ILogger<LogDownloadController> _logger = logger;
    private readonly ITemporaryTokenService _tokenService = tokenService;
    private readonly ILogDownloadService _logDownloadService = logDownloadService;

    #endregion

    #region Public Methods

    [HttpGet("logs")]
    public async Task<IActionResult> DownloadLogs([FromQuery] string token)
    {
        if (!_tokenService.ValidateAndConsumeToken(token))
        {
            _logger.LogWarning("Unauthorized access attempt to download logs - invalid or missing token");
            return Unauthorized("Authentication required");
        }

        try
        {
            var logStream = await _logDownloadService.CreateLogZipStreamAsync();
            var fileName = $"adwh-logs-{DateTime.Now.ToString(LogConstants.ArchiveDateTimeFormat)}{LogConstants.ZipFileExtension}";

            _logger.LogInformation("Starting streaming download of logs archive: {FileName}", fileName);

            return new FileCallbackResult(LogConstants.ZipMediaType, fileName, async (outputStream, context) =>
            {
                try
                {
                    await logStream.CopyToAsync(outputStream);
                    _logger.LogInformation("Successfully streamed logs archive: {FileName}", fileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while streaming logs archive: {FileName}", fileName);
                    throw;
                }
                finally
                {
                    await logStream.DisposeAsync();
                }
            });
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogWarning("Logs directory not found: {Message}", ex.Message);
            return NotFound("Logs directory not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating logs archive");
            return StatusCode(500, "Error creating logs archive");
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
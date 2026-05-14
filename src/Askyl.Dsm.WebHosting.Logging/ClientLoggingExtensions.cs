using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for client-side (WASM) events.
/// </summary>
public static partial class ClientLoggingExtensions
{
    /// <summary>
    /// Logs failure to load the license file.
    /// </summary>
    [LoggerMessage(EventId = 1900, Level = LogLevel.Warning, Message = "Failed to load license file: {FileName}")]
    public static partial void FailedToLoadLicenseFile(this ILogger logger, Exception ex, string fileName);
}

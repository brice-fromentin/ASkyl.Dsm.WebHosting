using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogLicenseService { }

/// <summary>
/// Structured logging extension methods for client-side (WASM) events.
/// </summary>
public static partial class ClientLoggingExtensions
{
    /// <summary>
    /// Logs failure to load the license file.
    /// </summary>
    [LoggerMessage(EventId = 1900, Level = LogLevel.Warning, Message = "Failed to load license file: {FileName}")]
    public static partial void FailedToLoadLicenseFile(
        this ILogger<ILogLicenseService> logger, Exception ex, string fileName);
}

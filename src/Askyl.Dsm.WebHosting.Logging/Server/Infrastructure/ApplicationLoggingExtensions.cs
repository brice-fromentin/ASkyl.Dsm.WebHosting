using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogApplication { }

/// <summary>
/// Structured logging extension methods for the application host itself.
/// </summary>
public static partial class ApplicationLoggingExtensions
{
    /// <summary>
    /// Logs the full version at startup, so every log archive names the build that wrote it.
    /// </summary>
    [LoggerMessage(EventId = 3000001, Level = LogLevel.Information, Message = "Application starting, version {Version}")]
    public static partial void ApplicationStarting(this ILogger<ILogApplication> logger, string version);
}

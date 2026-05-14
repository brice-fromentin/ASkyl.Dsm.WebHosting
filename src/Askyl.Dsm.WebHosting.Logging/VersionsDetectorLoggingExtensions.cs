using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogVersionsDetectorService { }

/// <summary>
/// Structured logging extension methods for .NET version detection operations.
/// </summary>
public static partial class VersionsDetectorLoggingExtensions
{
    /// <summary>
    /// Logs that dotnet executable was not found.
    /// </summary>
    [LoggerMessage(EventId = 1806, Level = LogLevel.Warning, Message = "dotnet executable not found at {DotnetPath}. Keeping existing cached data.")]
    public static partial void DotnetExecutableNotFound(this ILogger<ILogVersionsDetectorService> logger, string dotnetPath);

    /// <summary>
    /// Logs successful framework cache refresh.
    /// </summary>
    [LoggerMessage(EventId = 1807, Level = LogLevel.Debug, Message = "Successfully refreshed framework cache with {FrameworkCount} frameworks")]
    public static partial void FrameworkCacheRefreshed(this ILogger<ILogVersionsDetectorService> logger, int frameworkCount);

    /// <summary>
    /// Logs that dotnet --info returned empty output.
    /// </summary>
    [LoggerMessage(EventId = 1808, Level = LogLevel.Warning, Message = "dotnet --info returned empty output. Keeping existing cached data.")]
    public static partial void DotnetInfoEmptyOutput(this ILogger<ILogVersionsDetectorService> logger);

    /// <summary>
    /// Logs failure to refresh framework cache.
    /// </summary>
    [LoggerMessage(EventId = 1809, Level = LogLevel.Error, Message = "Failed to refresh framework cache. Keeping existing cached data.")]
    public static partial void FailedToRefreshFrameworkCache(this ILogger<ILogVersionsDetectorService> logger, Exception ex);
}

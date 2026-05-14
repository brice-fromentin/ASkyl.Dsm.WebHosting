using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>
/// Structured logging extension methods for process lifecycle events.
/// </summary>
public static partial class ProcessLoggingExtensions
{
    #region Site start/stop

    /// <summary>
    /// Logs failure to start site due to disposal.
    /// </summary>
    [LoggerMessage(EventId = 1300, Level = LogLevel.Warning, Message = "Cannot start site '{SiteName}': lifecycle manager is disposing")]
    public static partial void CannotStartSiteDisposing(this ILogger logger, string siteName);

    /// <summary>
    /// Logs failure to stop site due to disposal.
    /// </summary>
    [LoggerMessage(EventId = 1301, Level = LogLevel.Warning, Message = "Cannot stop site '{SiteName}': lifecycle manager is disposing")]
    public static partial void CannotStopSiteDisposing(this ILogger logger, string siteName);

    /// <summary>
    /// Logs that a site is already running.
    /// </summary>
    [LoggerMessage(EventId = 1302, Level = LogLevel.Warning, Message = "Site '{SiteName}' is already running")]
    public static partial void SiteAlreadyRunning(this ILogger logger, string siteName);

    /// <summary>
    /// Logs that the application binary was not found.
    /// </summary>
    [LoggerMessage(EventId = 1303, Level = LogLevel.Error, Message = "Application binary not found: {ApplicationPath}")]
    public static partial void ApplicationBinaryNotFound(this ILogger logger, string applicationPath);

    /// <summary>
    /// Logs successful site start with PID.
    /// </summary>
    [LoggerMessage(EventId = 1304, Level = LogLevel.Information, Message = "Site '{SiteName}' started with PID {ProcessId}")]
    public static partial void SiteStarted(this ILogger logger, string siteName, int processId);

    /// <summary>
    /// Logs failure to start site.
    /// </summary>
    [LoggerMessage(EventId = 1305, Level = LogLevel.Error, Message = "Failed to start site: {SiteName}")]
    public static partial void FailedToStartSite(this ILogger logger, Exception ex, string siteName);

    #endregion

    #region Site stop

    /// <summary>
    /// Logs that a site is already stopped (idempotent operation).
    /// </summary>
    [LoggerMessage(EventId = 1306, Level = LogLevel.Warning, Message = "Site '{SiteName}' is already stopped (idempotent operation)")]
    public static partial void SiteAlreadyStopped(this ILogger logger, string siteName);

    /// <summary>
    /// Logs successful site stop.
    /// </summary>
    [LoggerMessage(EventId = 1307, Level = LogLevel.Information, Message = "Site '{SiteName}' stopped successfully")]
    public static partial void SiteStopped(this ILogger logger, string siteName);

    /// <summary>
    /// Logs that the site process no longer exists.
    /// </summary>
    [LoggerMessage(EventId = 1308, Level = LogLevel.Warning, Message = "Site '{SiteName}' process no longer exists.")]
    public static partial void SiteProcessNotFound(this ILogger logger, Exception ex, string siteName);

    /// <summary>
    /// Logs failure to stop site.
    /// </summary>
    [LoggerMessage(EventId = 1309, Level = LogLevel.Error, Message = "Failed to stop site: {SiteName}")]
    public static partial void FailedToStopSite(this ILogger logger, Exception ex, string siteName);

    #endregion

    #region Dispose

    /// <summary>
    /// Logs disposal of the lifecycle manager.
    /// </summary>
    [LoggerMessage(EventId = 1310, Level = LogLevel.Information, Message = "Disposing lifecycle manager for site '{SiteName}'")]
    public static partial void DisposingLifecycleManager(this ILogger logger, string siteName);

    /// <summary>
    /// Logs force kill of process during dispose.
    /// </summary>
    [LoggerMessage(EventId = 1311, Level = LogLevel.Warning, Message = "Force killing process during dispose for site '{SiteName}'")]
    public static partial void ForceKillingProcessOnDispose(this ILogger logger, string siteName);

    /// <summary>
    /// Logs failure to kill process during dispose.
    /// </summary>
    [LoggerMessage(EventId = 1312, Level = LogLevel.Error, Message = "Failed to kill process during dispose for site '{SiteName}'")]
    public static partial void FailedToKillProcessOnDispose(this ILogger logger, Exception ex, string siteName);

    #endregion

    #region Graceful shutdown

    /// <summary>
    /// Logs that the site process was already dead during cleanup.
    /// </summary>
    [LoggerMessage(EventId = 1313, Level = LogLevel.Warning, Message = "Site '{SiteName}' process was already dead. Cleaning up state.")]
    public static partial void ProcessAlreadyDead(this ILogger logger, string siteName);

    /// <summary>
    /// Logs sending SIGTERM signal to the site process.
    /// </summary>
    [LoggerMessage(EventId = 1314, Level = LogLevel.Information, Message = "Site '{SiteName}' sent SIGTERM (timeout {Timeout}s)")]
    public static partial void SentSigTerm(this ILogger logger, string siteName, int timeout);

    /// <summary>
    /// Logs failure to stop gracefully, triggering force kill.
    /// </summary>
    [LoggerMessage(EventId = 1315, Level = LogLevel.Warning, Message = "Site '{SiteName}' did not stop gracefully. Force killing process.")]
    public static partial void DidNotStopGracefully(this ILogger logger, string siteName);

    /// <summary>
    /// Logs failure to force kill the process.
    /// </summary>
    [LoggerMessage(EventId = 1316, Level = LogLevel.Error, Message = "Failed to force kill process for site '{SiteName}'. Process may still be running.")]
    public static partial void FailedToForceKill(this ILogger logger, Exception ex, string siteName);

    #endregion
}

using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogSiteLifecycleManager { }

/// <summary>
/// Structured logging extension methods for process lifecycle events.
/// </summary>
public static partial class ProcessLoggingExtensions
{
    #region Site start/stop (1600001–1600009)

    /// <summary>
    /// Logs failure to start site due to disposal.
    /// </summary>
    [LoggerMessage(EventId = 1600001, Level = LogLevel.Warning, Message = "Cannot start site '{SiteName}': lifecycle manager is disposing")]
    public static partial void CannotStartSiteDisposing(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs failure to stop site due to disposal.
    /// </summary>
    [LoggerMessage(EventId = 1600002, Level = LogLevel.Warning, Message = "Cannot stop site '{SiteName}': lifecycle manager is disposing")]
    public static partial void CannotStopSiteDisposing(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs that a site is already running.
    /// </summary>
    [LoggerMessage(EventId = 1600003, Level = LogLevel.Warning, Message = "Site '{SiteName}' is already running")]
    public static partial void SiteAlreadyRunning(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs that the application binary was not found.
    /// </summary>
    [LoggerMessage(EventId = 1600004, Level = LogLevel.Error, Message = "Application binary not found: {ApplicationPath}")]
    public static partial void ApplicationBinaryNotFound(this ILogger<ILogSiteLifecycleManager> logger, string applicationPath);

    /// <summary>
    /// Logs that site start was blocked due to incompatible framework.
    /// </summary>
    [LoggerMessage(EventId = 1600005, Level = LogLevel.Warning, Message = "Cannot start site: {Reason}")]
    public static partial void SiteStartBlockedIncompatible(this ILogger<ILogSiteLifecycleManager> logger, string reason);

    /// <summary>
    /// Logs successful site start with PID.
    /// </summary>
    [LoggerMessage(EventId = 1600006, Level = LogLevel.Information, Message = "Site '{SiteName}' started with PID {ProcessId}")]
    public static partial void SiteStarted(this ILogger<ILogSiteLifecycleManager> logger, string siteName, int processId);

    /// <summary>
    /// Logs failure to start site.
    /// </summary>
    [LoggerMessage(EventId = 1600007, Level = LogLevel.Error, Message = "Failed to start site: {SiteName}")]
    public static partial void FailedToStartSite(this ILogger<ILogSiteLifecycleManager> logger, Exception ex, string siteName);

    #endregion

    #region Site stop (1601001–1601004)

    /// <summary>
    /// Logs that a site is already stopped (idempotent operation).
    /// </summary>
    [LoggerMessage(EventId = 1601001, Level = LogLevel.Warning, Message = "Site '{SiteName}' is already stopped (idempotent operation)")]
    public static partial void SiteAlreadyStopped(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs successful site stop.
    /// </summary>
    [LoggerMessage(EventId = 1601002, Level = LogLevel.Information, Message = "Site '{SiteName}' stopped successfully")]
    public static partial void SiteStopped(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs that the site process no longer exists.
    /// </summary>
    [LoggerMessage(EventId = 1601003, Level = LogLevel.Warning, Message = "Site '{SiteName}' process no longer exists.")]
    public static partial void SiteProcessNotFound(this ILogger<ILogSiteLifecycleManager> logger, Exception ex, string siteName);

    /// <summary>
    /// Logs failure to stop site.
    /// </summary>
    [LoggerMessage(EventId = 1601004, Level = LogLevel.Error, Message = "Failed to stop site: {SiteName}")]
    public static partial void FailedToStopSite(this ILogger<ILogSiteLifecycleManager> logger, Exception ex, string siteName);

    #endregion

    #region Dispose (1602001–1602003)

    /// <summary>
    /// Logs disposal of the lifecycle manager.
    /// </summary>
    [LoggerMessage(EventId = 1602001, Level = LogLevel.Information, Message = "Disposing lifecycle manager for site '{SiteName}'")]
    public static partial void DisposingLifecycleManager(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs force kill of process during dispose.
    /// </summary>
    [LoggerMessage(EventId = 1602002, Level = LogLevel.Warning, Message = "Force killing process during dispose for site '{SiteName}'")]
    public static partial void ForceKillingProcessOnDispose(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs failure to kill process during dispose.
    /// </summary>
    [LoggerMessage(EventId = 1602003, Level = LogLevel.Error, Message = "Failed to kill process during dispose for site '{SiteName}'")]
    public static partial void FailedToKillProcessOnDispose(this ILogger<ILogSiteLifecycleManager> logger, Exception ex, string siteName);

    #endregion

    #region Graceful shutdown (1603001–1603004)

    /// <summary>
    /// Logs that the site process was already dead during cleanup.
    /// </summary>
    [LoggerMessage(EventId = 1603001, Level = LogLevel.Warning, Message = "Site '{SiteName}' process was already dead. Cleaning up state.")]
    public static partial void ProcessAlreadyDead(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs sending SIGTERM signal to the site process.
    /// </summary>
    [LoggerMessage(EventId = 1603002, Level = LogLevel.Information, Message = "Site '{SiteName}' sent SIGTERM (timeout {Timeout}s)")]
    public static partial void SentSigTerm(this ILogger<ILogSiteLifecycleManager> logger, string siteName, int timeout);

    /// <summary>
    /// Logs failure to stop gracefully, triggering force kill.
    /// </summary>
    [LoggerMessage(EventId = 1603003, Level = LogLevel.Warning, Message = "Site '{SiteName}' did not stop gracefully. Force killing process.")]
    public static partial void DidNotStopGracefully(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs failure to force kill the process.
    /// </summary>
    [LoggerMessage(EventId = 1603004, Level = LogLevel.Error, Message = "Failed to force kill process for site '{SiteName}'. Process may still be running.")]
    public static partial void FailedToForceKill(this ILogger<ILogSiteLifecycleManager> logger, Exception ex, string siteName);

    #endregion

    #region Duration (1604001–1604005)

    /// <summary>
    /// Logs the start of a site start operation.
    /// </summary>
    [LoggerMessage(EventId = 1604001, Level = LogLevel.Debug, Message = "Attempting to start site '{SiteName}'")]
    public static partial void StartAttempt(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs the start of a site stop operation.
    /// </summary>
    [LoggerMessage(EventId = 1604002, Level = LogLevel.Debug, Message = "Attempting to stop site '{SiteName}'")]
    public static partial void StopAttempt(this ILogger<ILogSiteLifecycleManager> logger, string siteName);

    /// <summary>
    /// Logs the duration of a site start operation.
    /// </summary>
    [LoggerMessage(EventId = 1604003, Level = LogLevel.Debug, Message = "Site '{SiteName}' start completed in {Duration}ms")]
    public static partial void StartDuration(this ILogger<ILogSiteLifecycleManager> logger, string siteName, long duration);

    /// <summary>
    /// Logs the duration of a site stop operation.
    /// </summary>
    [LoggerMessage(EventId = 1604004, Level = LogLevel.Debug, Message = "Site '{SiteName}' stop completed in {Duration}ms")]
    public static partial void StopDuration(this ILogger<ILogSiteLifecycleManager> logger, string siteName, long duration);

    /// <summary>
    /// Logs that a process did not exit within the specified timeout.
    /// </summary>
    [LoggerMessage(EventId = 1604005, Level = LogLevel.Warning, Message = "Site '{SiteName}' process {ProcessId} did not exit within {TimeoutMs}ms")]
    public static partial void ProcessWaitTimeout(this ILogger<ILogSiteLifecycleManager> logger, string siteName, int processId, long timeoutMs);

    #endregion
}

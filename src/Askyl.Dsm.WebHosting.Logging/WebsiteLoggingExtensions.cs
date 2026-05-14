using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogWebSiteHostingService { }

/// <summary>
/// Structured logging extension methods for website hosting events.
/// </summary>
public static partial class WebsiteLoggingExtensions
{
    #region Add website

    /// <summary>
    /// Logs permission setting failure during website addition.
    /// </summary>
    [LoggerMessage(EventId = 1500, Level = LogLevel.Error, Message = "Permission setting failed for '{SiteName}': {ErrorMessage}")]
    public static partial void PermissionSettingFailedAdd(this ILogger<ILogWebSiteHostingService> logger, string siteName, string? errorMessage);

    /// <summary>
    /// Logs reverse proxy creation failure during website addition.
    /// </summary>
    [LoggerMessage(EventId = 1501, Level = LogLevel.Error, Message = "Reverse proxy creation failed for '{SiteName}': {ErrorMessage}")]
    public static partial void ReverseProxyCreationFailedAdd(this ILogger<ILogWebSiteHostingService> logger, string siteName, string? errorMessage);

    /// <summary>
    /// Logs error during website addition.
    /// </summary>
    [LoggerMessage(EventId = 1502, Level = LogLevel.Error, Message = "Error adding website: {SiteName}")]
    public static partial void ErrorAddingWebsite(this ILogger<ILogWebSiteHostingService> logger, Exception ex, string siteName);

    #endregion

    #region Update website

    /// <summary>
    /// Logs that no instance was found for site during update.
    /// </summary>
    [LoggerMessage(EventId = 1503, Level = LogLevel.Error, Message = "Instance not found for site: {SiteName}")]
    public static partial void InstanceNotFoundUpdate(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs permission setting failure during website update.
    /// </summary>
    [LoggerMessage(EventId = 1504, Level = LogLevel.Error, Message = "Permission setting failed for '{SiteName}': {ErrorMessage}")]
    public static partial void PermissionSettingFailedUpdate(this ILogger<ILogWebSiteHostingService> logger, string siteName, string? errorMessage);

    /// <summary>
    /// Logs reverse proxy update failure during website update.
    /// </summary>
    [LoggerMessage(EventId = 1505, Level = LogLevel.Error, Message = "Reverse proxy update failed for '{SiteName}': {ErrorMessage}")]
    public static partial void ReverseProxyUpdateFailed(this ILogger<ILogWebSiteHostingService> logger, string siteName, string? errorMessage);

    /// <summary>
    /// Logs error during website update.
    /// </summary>
    [LoggerMessage(EventId = 1506, Level = LogLevel.Error, Message = "Error updating website: {SiteName}")]
    public static partial void ErrorUpdatingWebsite(this ILogger<ILogWebSiteHostingService> logger, Exception ex, string siteName);

    #endregion

    #region Start/stop site

    /// <summary>
    /// Logs that site cannot be started (not found).
    /// </summary>
    [LoggerMessage(EventId = 1507, Level = LogLevel.Warning, Message = "Cannot start site: site with ID {InstanceId} not found")]
    public static partial void CannotStartSiteNotFound(this ILogger<ILogWebSiteHostingService> logger, Guid instanceId);

    /// <summary>
    /// Logs that lifecycle manager was not found for site start.
    /// </summary>
    [LoggerMessage(EventId = 1508, Level = LogLevel.Error, Message = "Lifecycle manager not found for site: {SiteId}")]
    public static partial void LifecycleManagerNotFoundStart(this ILogger<ILogWebSiteHostingService> logger, Guid siteId);

    /// <summary>
    /// Logs that site cannot be stopped (not found).
    /// </summary>
    [LoggerMessage(EventId = 1509, Level = LogLevel.Warning, Message = "Cannot stop site: site with ID {InstanceId} not found")]
    public static partial void CannotStopSiteNotFound(this ILogger<ILogWebSiteHostingService> logger, Guid instanceId);

    /// <summary>
    /// Logs that lifecycle manager was not found for site stop.
    /// </summary>
    [LoggerMessage(EventId = 1510, Level = LogLevel.Error, Message = "Lifecycle manager not found for site: {SiteId}")]
    public static partial void LifecycleManagerNotFoundStop(this ILogger<ILogWebSiteHostingService> logger, Guid siteId);

    #endregion

    #region Service lifecycle

    /// <summary>
    /// Logs that the WebSite hosting service is starting.
    /// </summary>
    [LoggerMessage(EventId = 1511, Level = LogLevel.Information, Message = "WebSite hosting service starting")]
    public static partial void HostingServiceStarting(this ILogger<ILogWebSiteHostingService> logger);

    /// <summary>
    /// Logs that the WebSite hosting service has started.
    /// </summary>
    [LoggerMessage(EventId = 1512, Level = LogLevel.Information, Message = "WebSite hosting service started")]
    public static partial void HostingServiceStarted(this ILogger<ILogWebSiteHostingService> logger);

    /// <summary>
    /// Logs stopping of all websites.
    /// </summary>
    [LoggerMessage(EventId = 1513, Level = LogLevel.Information, Message = "Stopping all websites")]
    public static partial void StoppingAllWebsites(this ILogger<ILogWebSiteHostingService> logger);

    #endregion

    #region Instance management

    /// <summary>
    /// Logs instance creation for a site.
    /// </summary>
    [LoggerMessage(EventId = 1514, Level = LogLevel.Information, Message = "Instance created for site: {SiteName}")]
    public static partial void InstanceCreated(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs total instances initialized.
    /// </summary>
    [LoggerMessage(EventId = 1515, Level = LogLevel.Information, Message = "All instances initialized: {Count} sites")]
    public static partial void AllInstancesInitialized(this ILogger<ILogWebSiteHostingService> logger, int count);

    /// <summary>
    /// Logs sites that failed to start.
    /// </summary>
    [LoggerMessage(EventId = 1516, Level = LogLevel.Warning, Message = "{Count} site(s) failed to start: {Failures}")]
    public static partial void SitesFailedToStart(this ILogger<ILogWebSiteHostingService> logger, int count, string failures);

    /// <summary>
    /// Logs instance addition for a site.
    /// </summary>
    [LoggerMessage(EventId = 1517, Level = LogLevel.Information, Message = "Instance added for site: {SiteName}")]
    public static partial void InstanceAdded(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs that an existing instance was not found during update.
    /// </summary>
    [LoggerMessage(EventId = 1518, Level = LogLevel.Error, Message = "Instance of site: {SiteName} not found.")]
    public static partial void InstanceNotFoundDuringUpdate(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs site stopping due to disable or restart requirement.
    /// </summary>
    [LoggerMessage(EventId = 1519, Level = LogLevel.Information, Message = "Stopping site: {SiteName} (disabled or restart required)")]
    public static partial void StoppingSiteDisabledOrRestart(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs instance update for a site.
    /// </summary>
    [LoggerMessage(EventId = 1520, Level = LogLevel.Information, Message = "Instance updated for site: {SiteName}")]
    public static partial void InstanceUpdated(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    #endregion

    #region Remove website

    /// <summary>
    /// Logs that instance cannot be removed (not found).
    /// </summary>
    [LoggerMessage(EventId = 1521, Level = LogLevel.Warning, Message = "Cannot remove instance: instance {InstanceId} not found")]
    public static partial void CannotRemoveInstanceNotFound(this ILogger<ILogWebSiteHostingService> logger, Guid instanceId);

    /// <summary>
    /// Logs failure to stop site before deletion.
    /// </summary>
    [LoggerMessage(EventId = 1522, Level = LogLevel.Warning, Message = "Failed to stop site before deletion: {ErrorMessage}")]
    public static partial void FailedToStopBeforeDeletion(this ILogger<ILogWebSiteHostingService> logger, string? errorMessage);

    /// <summary>
    /// Logs reverse proxy deletion failure (non-blocking).
    /// </summary>
    [LoggerMessage(EventId = 1523, Level = LogLevel.Warning, Message = "Reverse proxy deletion failed for '{SiteName}' (site will be removed anyway): {ErrorMessage}")]
    public static partial void ReverseProxyDeletionFailed(this ILogger<ILogWebSiteHostingService> logger, string siteName, string? errorMessage);

    /// <summary>
    /// Logs instance removal for a site.
    /// </summary>
    [LoggerMessage(EventId = 1524, Level = LogLevel.Information, Message = "Instance removed for site: {SiteName}")]
    public static partial void InstanceRemoved(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs error during site removal.
    /// </summary>
    [LoggerMessage(EventId = 1525, Level = LogLevel.Error, Message = "Failed to remove site: {SiteName}")]
    public static partial void FailedToRemoveSite(this ILogger<ILogWebSiteHostingService> logger, Exception ex, string siteName);

    #endregion

    #region Permissions

    /// <summary>
    /// Logs that no application real path is configured for permission setting.
    /// </summary>
    [LoggerMessage(EventId = 1526, Level = LogLevel.Warning, Message = "No application real path configured for '{SiteName}', skipping permission setting")]
    public static partial void NoApplicationRealPath(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs setting of HTTP group permissions for a site.
    /// </summary>
    [LoggerMessage(EventId = 1527, Level = LogLevel.Debug, Message = "Setting HTTP group permissions for '{SiteName}' at path: {Path} (IsDirectory: {IsDirectory})")]
    public static partial void SettingHttpGroupPermissions(this ILogger<ILogWebSiteHostingService> logger, string siteName, string path, bool isDirectory);

    #endregion

    #region Reverse proxy rules

    /// <summary>
    /// Logs successful reverse proxy rule creation.
    /// </summary>
    [LoggerMessage(EventId = 1528, Level = LogLevel.Information, Message = "Reverse proxy rule created for site '{SiteName}'")]
    public static partial void ReverseProxyRuleCreated(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs error creating reverse proxy rule.
    /// </summary>
    [LoggerMessage(EventId = 1529, Level = LogLevel.Error, Message = "Failed to create reverse proxy rule for '{SiteName}'")]
    public static partial void FailedToCreateReverseProxyRule(this ILogger<ILogWebSiteHostingService> logger, Exception ex, string siteName);

    /// <summary>
    /// Logs successful reverse proxy rule update.
    /// </summary>
    [LoggerMessage(EventId = 1530, Level = LogLevel.Information, Message = "Reverse proxy rule updated for '{SiteName}'")]
    public static partial void ReverseProxyRuleUpdated(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs error updating reverse proxy rule.
    /// </summary>
    [LoggerMessage(EventId = 1531, Level = LogLevel.Error, Message = "Failed to update reverse proxy rule for '{SiteName}'")]
    public static partial void FailedToUpdateReverseProxyRule(this ILogger<ILogWebSiteHostingService> logger, Exception ex, string siteName);

    /// <summary>
    /// Logs successful reverse proxy rule deletion.
    /// </summary>
    [LoggerMessage(EventId = 1532, Level = LogLevel.Information, Message = "Reverse proxy rule deleted for site '{SiteName}'")]
    public static partial void ReverseProxyRuleDeleted(this ILogger<ILogWebSiteHostingService> logger, string siteName);

    /// <summary>
    /// Logs error deleting reverse proxy rule.
    /// </summary>
    [LoggerMessage(EventId = 1533, Level = LogLevel.Error, Message = "Failed to delete reverse proxy rule for '{SiteName}'")]
    public static partial void FailedToDeleteReverseProxyRule(this ILogger<ILogWebSiteHostingService> logger, Exception ex, string siteName);

    #endregion
}

using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogReverseProxyManagerService { }

/// <summary>
/// Structured logging extension methods for reverse proxy events.
/// </summary>
public static partial class ReverseProxyLoggingExtensions
{
    #region Create

    /// <summary>
    /// Logs creation of a reverse proxy for a site.
    /// </summary>
    [LoggerMessage(EventId = 1700001, Level = LogLevel.Information, Message = "Creating reverse proxy for site {SiteName}")]
    public static partial void CreatingReverseProxy(this ILogger<ILogReverseProxyManagerService> logger, string siteName);

    /// <summary>
    /// Logs that a reverse proxy already exists for the site (idempotency).
    /// </summary>
    [LoggerMessage(EventId = 1700002, Level = LogLevel.Warning, Message = "Reverse proxy already exists for site {SiteName} with UUID {Uuid}.")]
    public static partial void ReverseProxyAlreadyExists(this ILogger<ILogReverseProxyManagerService> logger, string siteName, Guid? uuid);

    /// <summary>
    /// Logs failure to create a reverse proxy with API error code.
    /// </summary>
    [LoggerMessage(EventId = 1700003, Level = LogLevel.Error, Message = "Failed to create reverse proxy for site {SiteName}. API error code: {ApiErrorCode}")]
    public static partial void FailedToCreateReverseProxy(this ILogger<ILogReverseProxyManagerService> logger, string siteName, int? apiErrorCode);

    /// <summary>
    /// Logs that reverse proxy creation succeeded but verification failed.
    /// </summary>
    [LoggerMessage(EventId = 1700004, Level = LogLevel.Error, Message = "Reverse proxy creation succeeded but could not verify existence for site {SiteName}")]
    public static partial void ReverseProxyCreationNotVerified(this ILogger<ILogReverseProxyManagerService> logger, string siteName);

    /// <summary>
    /// Logs successful reverse proxy creation with UUID.
    /// </summary>
    [LoggerMessage(EventId = 1700005, Level = LogLevel.Information, Message = "Reverse proxy created successfully for site {SiteName} with UUID {Uuid}")]
    public static partial void ReverseProxyCreated(this ILogger<ILogReverseProxyManagerService> logger, string siteName, Guid? uuid);

    #endregion

    #region Update

    /// <summary>
    /// Logs update of a reverse proxy for a site.
    /// </summary>
    [LoggerMessage(EventId = 1700006, Level = LogLevel.Information, Message = "Updating reverse proxy for site {SiteName}")]
    public static partial void UpdatingReverseProxy(this ILogger<ILogReverseProxyManagerService> logger, string siteName);

    /// <summary>
    /// Logs successful reverse proxy update.
    /// </summary>
    [LoggerMessage(EventId = 1700007, Level = LogLevel.Information, Message = "Reverse proxy updated successfully for site {SiteName}")]
    public static partial void ReverseProxyUpdated(this ILogger<ILogReverseProxyManagerService> logger, string siteName);

    #endregion

    #region Delete

    /// <summary>
    /// Logs that no reverse proxy was found for deletion.
    /// </summary>
    [LoggerMessage(EventId = 1700008, Level = LogLevel.Information, Message = "No reverse proxy found for site {SiteName}. Nothing to delete.")]
    public static partial void NoReverseProxyToDelete(this ILogger<ILogReverseProxyManagerService> logger, string siteName);

    /// <summary>
    /// Logs deletion of a reverse proxy by UUID.
    /// </summary>
    [LoggerMessage(EventId = 1700009, Level = LogLevel.Information, Message = "Deleting reverse proxy {Uuid} for site {SiteName}")]
    public static partial void DeletingReverseProxy(this ILogger<ILogReverseProxyManagerService> logger, Guid? uuid, string siteName);

    /// <summary>
    /// Logs that the reverse proxy was already deleted externally.
    /// </summary>
    [LoggerMessage(EventId = 1700010, Level = LogLevel.Warning, Message = "Reverse proxy for site {SiteName} was already deleted.")]
    public static partial void ReverseProxyAlreadyDeleted(this ILogger<ILogReverseProxyManagerService> logger, string siteName);

    /// <summary>
    /// Logs successful reverse proxy deletion.
    /// </summary>
    [LoggerMessage(EventId = 1700011, Level = LogLevel.Information, Message = "Deleted reverse proxy {Uuid} successfully")]
    public static partial void ReverseProxyDeleted(this ILogger<ILogReverseProxyManagerService> logger, Guid? uuid);

    #endregion

    #region Failure

    /// <summary>
    /// Logs failure to update a reverse proxy.
    /// </summary>
    [LoggerMessage(EventId = 1700012, Level = LogLevel.Error, Message = "Failed to update reverse proxy for site {SiteName}")]
    public static partial void FailedToUpdateReverseProxy(this ILogger<ILogReverseProxyManagerService> logger, Exception ex, string siteName);

    /// <summary>
    /// Logs failure to delete a reverse proxy.
    /// </summary>
    [LoggerMessage(EventId = 1700013, Level = LogLevel.Error, Message = "Failed to delete reverse proxy {Uuid} for site {SiteName}")]
    public static partial void FailedToDeleteReverseProxy(this ILogger<ILogReverseProxyManagerService> logger, Exception ex, Guid? uuid, string siteName);

    #endregion
}

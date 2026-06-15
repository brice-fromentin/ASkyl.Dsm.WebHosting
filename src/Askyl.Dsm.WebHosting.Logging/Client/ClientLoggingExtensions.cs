using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogLicenseService { }

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogHome { }

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogAspNetReleasesDialog { }

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogFileSelectionDialog { }

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogDotnetVersionsDialog { }

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogWebSiteConfigurationDialog { }

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogCultureManager { }

/// <summary>
/// Structured logging extension methods for client-side (WASM) events.
/// </summary>
public static partial class ClientLoggingExtensions
{
    #region LicenseService — 7000001

    /// <summary>
    /// Logs failure to load the license file.
    /// </summary>
    [LoggerMessage(EventId = 7000001, Level = LogLevel.Warning, Message = "Failed to load license file: {FileName}")]
    public static partial void FailedToLoadLicenseFile(this ILogger<ILogLicenseService> logger, Exception ex, string fileName);

    #endregion

    #region Home — 7100001–7100015

    /// <summary>
    /// Logs a failure to load websites from the hosting service.
    /// </summary>
    [LoggerMessage(EventId = 7100001, Level = LogLevel.Warning, Message = "Failed to load websites: {Message}")]
    public static partial void FailedToLoadWebsites(this ILogger<ILogHome> logger, string message);

    /// <summary>
    /// Logs the count of successfully loaded website instances.
    /// </summary>
    [LoggerMessage(EventId = 7100002, Level = LogLevel.Information, Message = "Loaded {WebsiteCount} website instances")]
    public static partial void LoadedWebsites(this ILogger<ILogHome> logger, int websiteCount);

    /// <summary>
    /// Logs an exception thrown while loading websites.
    /// </summary>
    [LoggerMessage(EventId = 7100003, Level = LogLevel.Error, Message = "Error loading websites")]
    public static partial void ErrorLoadingWebsites(this ILogger<ILogHome> logger, Exception ex);

    /// <summary>
    /// Logs a failure to delete a website.
    /// </summary>
    [LoggerMessage(EventId = 7100004, Level = LogLevel.Warning, Message = "Failed to delete website: {ErrorMessage}")]
    public static partial void FailedToDeleteWebsite(this ILogger<ILogHome> logger, string errorMessage);

    /// <summary>
    /// Logs successful deletion of a website.
    /// </summary>
    [LoggerMessage(EventId = 7100005, Level = LogLevel.Information, Message = "Website '{WebsiteName}' deleted successfully")]
    public static partial void WebsiteDeleted(this ILogger<ILogHome> logger, string websiteName);

    /// <summary>
    /// Logs an exception thrown while deleting a website.
    /// </summary>
    [LoggerMessage(EventId = 7100006, Level = LogLevel.Error, Message = "Error deleting website")]
    public static partial void ErrorDeletingWebsite(this ILogger<ILogHome> logger, Exception ex);

    /// <summary>
    /// Logs a failure to start a website.
    /// </summary>
    [LoggerMessage(EventId = 7100007, Level = LogLevel.Warning, Message = "Failed to start website: {ErrorMessage}")]
    public static partial void FailedToStartWebsite(this ILogger<ILogHome> logger, string errorMessage);

    /// <summary>
    /// Logs successful start of a website.
    /// </summary>
    [LoggerMessage(EventId = 7100008, Level = LogLevel.Information, Message = "Website '{WebsiteName}' started successfully")]
    public static partial void WebsiteStarted(this ILogger<ILogHome> logger, string websiteName);

    /// <summary>
    /// Logs an exception thrown while starting a website.
    /// </summary>
    [LoggerMessage(EventId = 7100009, Level = LogLevel.Error, Message = "Error starting website")]
    public static partial void ErrorStartingWebsite(this ILogger<ILogHome> logger, Exception ex);

    /// <summary>
    /// Logs a failure to stop a website.
    /// </summary>
    [LoggerMessage(EventId = 7100010, Level = LogLevel.Warning, Message = "Failed to stop website: {ErrorMessage}")]
    public static partial void FailedToStopWebsite(this ILogger<ILogHome> logger, string errorMessage);

    /// <summary>
    /// Logs successful stop of a website.
    /// </summary>
    [LoggerMessage(EventId = 7100011, Level = LogLevel.Information, Message = "Website '{WebsiteName}' stopped successfully")]
    public static partial void WebsiteStopped(this ILogger<ILogHome> logger, string websiteName);

    /// <summary>
    /// Logs an exception thrown while stopping a website.
    /// </summary>
    [LoggerMessage(EventId = 7100012, Level = LogLevel.Error, Message = "Error stopping website")]
    public static partial void ErrorStoppingWebsite(this ILogger<ILogHome> logger, Exception ex);

    /// <summary>
    /// Logs a failure during logout.
    /// </summary>
    [LoggerMessage(EventId = 7100013, Level = LogLevel.Warning, Message = "Logout failed: {ErrorMessage}")]
    public static partial void LogoutFailed(this ILogger<ILogHome> logger, string errorMessage);

    /// <summary>
    /// Logs successful user logout.
    /// </summary>
    [LoggerMessage(EventId = 7100014, Level = LogLevel.Information, Message = "User logged out successfully")]
    public static partial void LoggedOut(this ILogger<ILogHome> logger);

    /// <summary>
    /// Logs an exception thrown during logout.
    /// </summary>
    [LoggerMessage(EventId = 7100015, Level = LogLevel.Error, Message = "Error during logout")]
    public static partial void ErrorDuringLogout(this ILogger<ILogHome> logger, Exception ex);

    #endregion

    #region AspNetReleasesDialog — 7200001–7200004

    /// <summary>
    /// Logs an exception thrown while loading .NET channels.
    /// </summary>
    [LoggerMessage(EventId = 7200001, Level = LogLevel.Error, Message = "Failed to load .NET channels")]
    public static partial void FailedToLoadChannels(this ILogger<ILogAspNetReleasesDialog> logger, Exception ex);

    /// <summary>
    /// Logs an exception thrown while loading releases for a specific channel.
    /// </summary>
    [LoggerMessage(EventId = 7200002, Level = LogLevel.Error, Message = "Failed to load releases for channel {Channel}")]
    public static partial void FailedToLoadReleases(this ILogger<ILogAspNetReleasesDialog> logger, Exception ex, string? channel);

    /// <summary>
    /// Logs an exception thrown during framework installation.
    /// </summary>
    [LoggerMessage(EventId = 7200003, Level = LogLevel.Error, Message = "Installation error for framework {Version}")]
    public static partial void InstallationError(this ILogger<ILogAspNetReleasesDialog> logger, Exception ex, string? version);

    /// <summary>
    /// Logs an exception thrown during framework uninstallation.
    /// </summary>
    [LoggerMessage(EventId = 7200004, Level = LogLevel.Error, Message = "Uninstallation error for framework {Version}")]
    public static partial void UninstallationError(this ILogger<ILogAspNetReleasesDialog> logger, Exception ex, string? version);

    #endregion

    #region FileSelectionDialog — 7300001–7300004

    /// <summary>
    /// Logs an exception thrown while loading shared folders.
    /// </summary>
    [LoggerMessage(EventId = 7300001, Level = LogLevel.Error, Message = "Failed to load shared folders")]
    public static partial void FailedToLoadSharedFolders(this ILogger<ILogFileSelectionDialog> logger, Exception ex);

    /// <summary>
    /// Logs an exception thrown while loading child directories.
    /// </summary>
    [LoggerMessage(EventId = 7300002, Level = LogLevel.Error, Message = "Failed to load child directories for path {Path}")]
    public static partial void FailedToLoadChildDirectories(this ILogger<ILogFileSelectionDialog> logger, Exception ex, string path);

    /// <summary>
    /// Logs an exception thrown while loading directory contents.
    /// </summary>
    [LoggerMessage(EventId = 7300003, Level = LogLevel.Error, Message = "Failed to load directory contents for path {Path}")]
    public static partial void FailedToLoadDirectoryContents(this ILogger<ILogFileSelectionDialog> logger, Exception ex, string path);

    /// <summary>
    /// Logs a file double-click event.
    /// </summary>
    [LoggerMessage(EventId = 7300004, Level = LogLevel.Debug, Message = "File double-clicked: {FileName} (IsDirectory: {IsDirectory})")]
    public static partial void FileDoubleClicked(this ILogger<ILogFileSelectionDialog> logger, string fileName, bool isDirectory);

    #endregion

    #region DotnetVersionsDialog — 7400001

    /// <summary>
    /// Logs an exception thrown while searching for the global .NET version.
    /// </summary>
    [LoggerMessage(EventId = 7400001, Level = LogLevel.Error, Message = "Error while searching for global .NET version")]
    public static partial void ErrorSearchingDotnetVersion(this ILogger<ILogDotnetVersionsDialog> logger, Exception ex);

    #endregion

    #region WebSiteConfigurationDialog — 7500001

    /// <summary>
    /// Logs an exception thrown while creating or updating a website.
    /// </summary>
    [LoggerMessage(EventId = 7500001, Level = LogLevel.Error, Message = "Error {Action} website")]
    public static partial void ErrorModifyingWebsite(this ILogger<ILogWebSiteConfigurationDialog> logger, Exception ex, string action);

    #endregion

    #region CultureManager — 7600001–7600009

    /// <summary>
    /// Logs the resolved culture from the login response.
    /// </summary>
    [LoggerMessage(EventId = 7600001, Level = LogLevel.Debug, Message = "Culture resolved from login response: {Culture}")]
    public static partial void CultureResolvedFromLogin(this ILogger<ILogCultureManager> logger, string culture);

    /// <summary>
    /// Logs the browser language fallback detection.
    /// </summary>
    [LoggerMessage(EventId = 7600002, Level = LogLevel.Debug, Message = "Culture resolved from browser language: {Culture}")]
    public static partial void CultureResolvedFromBrowser(this ILogger<ILogCultureManager> logger, string culture);

    /// <summary>
    /// Logs the fallback to default culture.
    /// </summary>
    [LoggerMessage(EventId = 7600003, Level = LogLevel.Debug, Message = "Culture resolved to default: {Culture}")]
    public static partial void CultureResolvedToDefault(this ILogger<ILogCultureManager> logger, string culture);

    /// <summary>
    /// Logs the final culture applied to the thread.
    /// </summary>
    [LoggerMessage(EventId = 7600004, Level = LogLevel.Debug, Message = "Culture applied to thread: {Culture}")]
    public static partial void CultureApplied(this ILogger<ILogCultureManager> logger, string culture);

    /// <summary>
    /// Logs a failure to detect browser language via JS interop.
    /// </summary>
    [LoggerMessage(EventId = 7600005, Level = LogLevel.Debug, Message = "Failed to detect browser language via JS interop")]
    public static partial void BrowserLanguageDetectionFailed(this ILogger<ILogCultureManager> logger, Exception ex);

    /// <summary>
    /// Logs the culture resolved from DSM system settings.
    /// </summary>
    [LoggerMessage(EventId = 7600006, Level = LogLevel.Debug, Message = "Culture resolved from DSM system settings: {Culture}")]
    public static partial void CultureResolvedFromSystem(this ILogger<ILogCultureManager> logger, string culture);

    /// <summary>
    /// Logs the culture reset to DSM system resolution (on logout).
    /// </summary>
    [LoggerMessage(EventId = 7600007, Level = LogLevel.Debug, Message = "Culture reset to system resolution: {Culture}")]
    public static partial void CultureResetToSystem(this ILogger<ILogCultureManager> logger, string culture);

    /// <summary>
    /// Logs that an invalid date format was ignored and the culture default was kept.
    /// </summary>
    [LoggerMessage(EventId = 7600008, Level = LogLevel.Warning, Message = "Invalid date format ignored, using culture default: {DateFormat}")]
    public static partial void InvalidDateFormatIgnored(this ILogger<ILogCultureManager> logger, string dateFormat);

    /// <summary>
    /// Logs that an invalid time format was ignored and the culture default was kept.
    /// </summary>
    [LoggerMessage(EventId = 7600009, Level = LogLevel.Warning, Message = "Invalid time format ignored, using culture default: {TimeFormat}")]
    public static partial void InvalidTimeFormatIgnored(this ILogger<ILogCultureManager> logger, string timeFormat);

    /// <summary>
    /// Logs that the user's culture from login was not supported and fell back to system resolution.
    /// </summary>
    [LoggerMessage(EventId = 7600010, Level = LogLevel.Warning, Message = "User culture '{UserCulture}' not supported, falling back to '{FallbackCulture}'")]
    public static partial void UserCultureUnsupported(this ILogger<ILogCultureManager> logger, string userCulture, string fallbackCulture);

    #endregion
}

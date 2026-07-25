using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogLicenseService { }

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogCultureManager { }

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogClient { }

/// <summary>
/// Structured logging extension methods for client-side (WASM) events.
/// </summary>
public static partial class ClientLoggingExtensions
{
    #region Client Utilities — 7100001–7100010

    /// <summary>
    /// Logs a failed JS interop call.
    /// </summary>
    [LoggerMessage(EventId = 7100001, Level = LogLevel.Warning, Message = "JS interop call failed")]
    public static partial void JsInteropFailed(this ILogger<ILogClient> logger, Exception exception);

    #endregion

    #region LicenseService — 7000001

    /// <summary>
    /// Logs failure to load the license file.
    /// </summary>
    [LoggerMessage(EventId = 7000001, Level = LogLevel.Warning, Message = "Failed to load license file: {FileName}")]
    public static partial void FailedToLoadLicenseFile(this ILogger<ILogLicenseService> logger, Exception ex, string fileName);

    #endregion

    #region CultureManager — 7600001–7600010

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

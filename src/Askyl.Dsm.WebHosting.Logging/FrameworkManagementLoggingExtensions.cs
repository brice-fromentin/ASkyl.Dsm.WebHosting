using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogFrameworkManagementService { }

/// <summary>
/// Structured logging extension methods for .NET framework management events.
/// </summary>
public static partial class FrameworkManagementLoggingExtensions
{
    /// <summary>
    /// Logs a failed install attempt due to missing version parameter.
    /// </summary>
    [LoggerMessage(EventId = 1200, Level = LogLevel.Warning, Message = "Install failed - Version is required")]
    public static partial void InstallFailedVersionRequired(this ILogger<ILogFrameworkManagementService> logger);

    /// <summary>
    /// Logs successful ASP.NET Core installation.
    /// </summary>
    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "ASP.NET Core {Version} installed successfully")]
    public static partial void FrameworkInstalled(this ILogger<ILogFrameworkManagementService> logger, string version);

    /// <summary>
    /// Logs an error during ASP.NET Core installation.
    /// </summary>
    [LoggerMessage(EventId = 1202, Level = LogLevel.Error, Message = "Error while installing ASP.NET Core {Version}")]
    public static partial void FrameworkInstallError(this ILogger<ILogFrameworkManagementService> logger, Exception ex, string version);

    /// <summary>
    /// Logs a failed uninstall attempt due to missing version parameter.
    /// </summary>
    [LoggerMessage(EventId = 1203, Level = LogLevel.Warning, Message = "Uninstall failed - Version is required")]
    public static partial void UninstallFailedVersionRequired(this ILogger<ILogFrameworkManagementService> logger);

    /// <summary>
    /// Logs successful ASP.NET Core uninstallation.
    /// </summary>
    [LoggerMessage(EventId = 1204, Level = LogLevel.Information, Message = "ASP.NET Core {Version} uninstalled successfully")]
    public static partial void FrameworkUninstalled(this ILogger<ILogFrameworkManagementService> logger, string version);

    /// <summary>
    /// Logs an uninstall failure with specific error message.
    /// </summary>
    [LoggerMessage(EventId = 1205, Level = LogLevel.Warning, Message = "Uninstall failed - {Message}")]
    public static partial void UninstallFailed(this ILogger<ILogFrameworkManagementService> logger, string message);

    /// <summary>
    /// Logs an error during ASP.NET Core uninstallation.
    /// </summary>
    [LoggerMessage(EventId = 1206, Level = LogLevel.Error, Message = "Error while uninstalling ASP.NET Core {Version}")]
    public static partial void FrameworkUninstallError(this ILogger<ILogFrameworkManagementService> logger, Exception ex, string version);
}

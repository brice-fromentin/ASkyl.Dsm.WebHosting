using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogDotnetVersionService { }

/// <summary>
/// Structured logging extension methods for .NET version detection and channel management.
/// </summary>
public static partial class DotnetVersionServiceLoggingExtensions
{
    /// <summary>
    /// Logs failure to retrieve installed .NET versions.
    /// </summary>
    [LoggerMessage(EventId = 1207, Level = LogLevel.Error, Message = "Failed to retrieve installed .NET versions")]
    public static partial void FailedToGetInstalledVersions(this ILogger<ILogDotnetVersionService> logger, Exception ex);

    /// <summary>
    /// Logs failure to check if a .NET channel is installed.
    /// </summary>
    [LoggerMessage(EventId = 1208, Level = LogLevel.Error, Message = "Failed to check if channel {Channel} is installed")]
    public static partial void FailedToCheckChannelInstalled(this ILogger<ILogDotnetVersionService> logger, Exception ex, string channel);

    /// <summary>
    /// Logs failure to check if a .NET version is installed.
    /// </summary>
    [LoggerMessage(EventId = 1209, Level = LogLevel.Error, Message = "Failed to check if version {Version} is installed")]
    public static partial void FailedToCheckVersionInstalled(this ILogger<ILogDotnetVersionService> logger, Exception ex, string version);

    /// <summary>
    /// Logs querying available .NET channels.
    /// </summary>
    [LoggerMessage(EventId = 1210, Level = LogLevel.Debug, Message = "Querying available .NET channels")]
    public static partial void QueryingChannels(this ILogger<ILogDotnetVersionService> logger);

    /// <summary>
    /// Logs failure to retrieve available .NET channels.
    /// </summary>
    [LoggerMessage(EventId = 1211, Level = LogLevel.Error, Message = "Failed to retrieve available .NET channels")]
    public static partial void FailedToGetChannels(this ILogger<ILogDotnetVersionService> logger, Exception ex);

    /// <summary>
    /// Logs querying releases for a specific .NET channel.
    /// </summary>
    [LoggerMessage(EventId = 1212, Level = LogLevel.Debug, Message = "Querying releases for channel {Channel}")]
    public static partial void QueryingReleases(this ILogger<ILogDotnetVersionService> logger, string channel);

    /// <summary>
    /// Logs failure to retrieve releases for a specific .NET channel.
    /// </summary>
    [LoggerMessage(EventId = 1213, Level = LogLevel.Error, Message = "Failed to retrieve releases for channel {Channel}")]
    public static partial void FailedToGetReleases(this ILogger<ILogDotnetVersionService> logger, Exception ex, string channel);
}

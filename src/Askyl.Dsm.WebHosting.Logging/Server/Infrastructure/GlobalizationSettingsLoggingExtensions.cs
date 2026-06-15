using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogGlobalizationSettings { }

/// <summary>
/// Structured logging extension methods for globalization settings discovery.
/// </summary>
public static partial class GlobalizationSettingsLoggingExtensions
{
    /// <summary>
    /// Logs the start of supported culture discovery from satellite resources.
    /// </summary>
    [LoggerMessage(EventId = 2700001, Level = LogLevel.Information, Message = "Discovering supported cultures from assembly {AssemblyName}")]
    public static partial void DiscoveringCultures(this ILogger<ILogGlobalizationSettings> logger, string assemblyName);

    /// <summary>
    /// Logs a culture skipped because it exists in the file system but not in the runtime.
    /// </summary>
    [LoggerMessage(EventId = 2700002, Level = LogLevel.Debug, Message = "Culture '{CultureName}' exists in file system but not supported by runtime — skipping")]
    public static partial void CultureSkippedNotSupported(this ILogger<ILogGlobalizationSettings> logger, string cultureName);

    /// <summary>
    /// Logs the total number of supported cultures discovered.
    /// </summary>
    [LoggerMessage(EventId = 2700003, Level = LogLevel.Information, Message = "Discovered {CultureCount} supported cultures")]
    public static partial void CulturesDiscovered(this ILogger<ILogGlobalizationSettings> logger, int cultureCount);

    /// <summary>
    /// Logs that the DSM system culture has been set.
    /// </summary>
    [LoggerMessage(EventId = 2700004, Level = LogLevel.Information, Message = "System culture set to '{CultureName}'")]
    public static partial void SystemCultureSet(this ILogger<ILogGlobalizationSettings> logger, string cultureName);
}

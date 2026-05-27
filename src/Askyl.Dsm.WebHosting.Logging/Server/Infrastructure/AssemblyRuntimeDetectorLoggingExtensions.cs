using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Logging;

/// <summary>Category marker for ILogger&lt;T&gt; — no implementation required.</summary>
public interface ILogAssemblyRuntimeDetector { }

/// <summary>
/// Structured logging extension methods for assembly runtime detection operations.
/// </summary>
public static partial class AssemblyRuntimeDetectorLoggingExtensions
{
    /// <summary>
    /// Logs successful framework detection from assembly.
    /// </summary>
    [LoggerMessage(EventId = 2250001, Level = LogLevel.Debug, Message = "Detected framework {Channel} for assembly {AssemblyPath}")]
    public static partial void DetectedFramework(this ILogger<ILogAssemblyRuntimeDetector> logger, string channel, string assemblyPath);

    /// <summary>
    /// Logs that the assembly is not compatible with installed runtimes.
    /// </summary>
    [LoggerMessage(EventId = 2250002, Level = LogLevel.Warning, Message = "Assembly {AssemblyPath} requires .NET {Channel} which is not installed")]
    public static partial void FrameworkNotInstalled(this ILogger<ILogAssemblyRuntimeDetector> logger, string assemblyPath, string channel);

    /// <summary>
    /// Logs that the assembly is not a .NET assembly or the target framework could not be determined.
    /// </summary>
    [LoggerMessage(EventId = 2250003, Level = LogLevel.Warning, Message = "Could not detect target framework for assembly {AssemblyPath}")]
    public static partial void CouldNotDetectFramework(this ILogger<ILogAssemblyRuntimeDetector> logger, string assemblyPath);

    /// <summary>
    /// Logs that the assembly file could not be read.
    /// </summary>
    [LoggerMessage(EventId = 2250004, Level = LogLevel.Error, Message = "Failed to read assembly {AssemblyPath}")]
    public static partial void FailedToReadAssembly(this ILogger<ILogAssemblyRuntimeDetector> logger, string assemblyPath, Exception ex);

    /// <summary>
    /// Logs that no *.runtimeconfig.json was found in the assembly directory.
    /// </summary>
    [LoggerMessage(EventId = 2250005, Level = LogLevel.Debug, Message = "No *.runtimeconfig.json found in directory for assembly {AssemblyPath}")]
    public static partial void NoRuntimeConfigFile(this ILogger<ILogAssemblyRuntimeDetector> logger, string assemblyPath);
}

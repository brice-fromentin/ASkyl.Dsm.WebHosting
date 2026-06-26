using System.Text.Json;
using System.Text.RegularExpressions;
using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.Extensions.Logging;

namespace Askyl.Dsm.WebHosting.Tools.Runtime;

/// <summary>
/// Detects the required .NET runtime framework from <c>*.runtimeconfig.json</c> files
/// next to the assembly. This approach works reliably even when the assembly is published
/// (where <c>TargetFrameworkAttribute</c> is stripped by the linker/trimmer).
/// </summary>
public sealed partial class AssemblyRuntimeDetector(
    ILogger<ILogAssemblyRuntimeDetector> logger,
    IVersionsDetectorService versionsDetector) : IAssemblyRuntimeDetector
{
    #region Regex Patterns

    [GeneratedRegex(@"^net(\d+\.\d+)")]
    private static partial Regex NetVersionRegex();

    #endregion

    /// <inheritdoc/>
    public AssemblyRuntimeInfo? Detect(string assemblyPath)
    {
        try
        {
            var runtimeConfigPath = FindRuntimeConfigPath(assemblyPath);

            if (runtimeConfigPath is null)
            {
                logger.NoRuntimeConfigFile(assemblyPath);
                logger.CouldNotDetectFramework(assemblyPath);
                return null;
            }

            var frameworkVersion = ExtractFrameworkVersion(runtimeConfigPath);

            if (frameworkVersion is null)
            {
                logger.CouldNotDetectFramework(assemblyPath);
                return null;
            }

            var channel = ExtractChannelFromVersion(frameworkVersion);

            logger.DetectedFramework(channel, assemblyPath);

            var isCompatible = versionsDetector.IsChannelInstalled(channel, DotNetFrameworkTypes.AspNetCore);

            if (!isCompatible)
            {
                logger.FrameworkNotInstalled(assemblyPath, channel);
            }

            return new AssemblyRuntimeInfo(channel, isCompatible);
        }
        catch (Exception ex)
        {
            logger.FailedToReadAssembly(assemblyPath, ex);
            return null;
        }
    }

    private static string? FindRuntimeConfigPath(string assemblyPath)
    {
        var configPath = Path.ChangeExtension(assemblyPath, ".runtimeconfig.json");

        return File.Exists(configPath) ? configPath : null;
    }

    /// <summary>
    /// Normalizes a full version string (e.g., <c>"10.0.8"</c>) to a channel (e.g., <c>"10.0"</c>).
    /// Returns the input unchanged if it is already a two-part channel.
    /// </summary>
    private static string ExtractChannelFromVersion(string version)
    {
        var parts = version.Split('.');

        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : version;
    }

    private static string? ExtractFrameworkVersion(string runtimeConfigPath)
    {
        var json = File.ReadAllText(runtimeConfigPath);

        using var doc = JsonDocument.Parse(json);

        var runtimeOptions = doc.RootElement.GetProperty("runtimeOptions");

        // Try runtimeOptions.framework.version first
        if (runtimeOptions.TryGetProperty("framework", out var frameworkProp) &&
            frameworkProp.TryGetProperty("version", out var versionProp))
        {
            return versionProp.GetString();
        }

        // Fallback: extract channel from runtimeOptions.tf m
        return ExtractChannelFromTfm(runtimeOptions);
    }

    private static string? ExtractChannelFromTfm(JsonElement runtimeOptions)
    {
        var tfm = runtimeOptions.GetProperty("tfm").GetString();

        if (tfm is null)
        {
            return null;
        }

        var match = NetVersionRegex().Match(tfm);

        if (!match.Success)
        {
            return null;
        }

        return match.Groups[1].Value;
    }
}

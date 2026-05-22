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
        if (!File.Exists(assemblyPath))
        {
            return null;
        }

        try
        {
            var directory = Path.GetDirectoryName(assemblyPath) ?? throw new InvalidOperationException($"Assembly path has no directory component: {assemblyPath}");
            var runtimeConfigPath = FindRuntimeConfigPath(directory);

            if (runtimeConfigPath is null)
            {
                logger.NoRuntimeConfigFile(assemblyPath, directory);
                logger.CouldNotDetectFramework(assemblyPath);
                return null;
            }

            var frameworkVersion = ExtractFrameworkVersion(runtimeConfigPath);

            if (frameworkVersion is null)
            {
                logger.CouldNotDetectFramework(assemblyPath);
                return null;
            }

            logger.DetectedFramework(frameworkVersion, assemblyPath);

            var isCompatible = versionsDetector.IsChannelInstalled(frameworkVersion, DotNetFrameworkTypes.AspNetCore);

            if (!isCompatible)
            {
                logger.FrameworkNotInstalled(assemblyPath, frameworkVersion);
            }

            var missingMessage = isCompatible ? null : $"Requires .NET {frameworkVersion}, but this runtime is not installed";

            return new AssemblyRuntimeInfo(frameworkVersion, isCompatible, missingMessage);
        }
        catch (Exception ex)
        {
            logger.FailedToReadAssembly(assemblyPath, ex);
            return null;
        }
    }

    private static string? FindRuntimeConfigPath(string directory)
    {
        var configFiles = Directory.EnumerateFiles(directory, "*.runtimeconfig.json");

        return configFiles.FirstOrDefault();
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

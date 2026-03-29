using Askyl.Dsm.WebHosting.Data.Domain.Runtime;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Provides version detection for installed .NET frameworks and runtimes.
/// </summary>
public interface IVersionsDetectorService
{
    /// <summary>
    /// Gets the list of installed .NET framework versions.
    /// Uses cached data after first call for blazing fast performance.
    /// </summary>
    /// <returns>List of installed frameworks.</returns>
    Task<List<FrameworkInfo>> GetInstalledVersionsAsync();

    /// <summary>
    /// Checks if a specific channel is installed.
    /// </summary>
    /// <param name="channel">The channel to check (e.g., "8.0").</param>
    /// <param name="frameworkType">The framework type to check.</param>
    /// <returns>True if the channel is installed, false otherwise.</returns>
    bool IsChannelInstalled(string channel, string frameworkType = "ASP.NET Core");

    /// <summary>
    /// Checks if a specific version is installed.
    /// </summary>
    /// <param name="version">The version to check (e.g., "8.0.5").</param>
    /// <param name="frameworkType">The framework type to check.</param>
    /// <returns>True if the version is installed, false otherwise.</returns>
    bool IsVersionInstalled(string version, string frameworkType = "ASP.NET Core");

    /// <summary>
    /// Forces a cache refresh by re-executing dotnet --info.
    /// Call this after install/uninstall operations to update cached data.
    /// </summary>
    Task RefreshCacheAsync();
}

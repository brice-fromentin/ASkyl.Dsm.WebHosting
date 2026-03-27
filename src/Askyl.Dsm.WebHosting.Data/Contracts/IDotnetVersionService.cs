using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Facade service for .NET version management operations.
/// </summary>
public interface IDotnetVersionService
{
    /// <summary>
    /// Gets the list of installed .NET versions on the system.
    /// </summary>
    /// <returns>An InstalledVersionsResult containing a list of FrameworkInfo objects representing installed frameworks.</returns>
    Task<InstalledVersionsResult> GetInstalledVersionsAsync();

    /// <summary>
    /// Checks if a specific channel is installed for a given framework type.
    /// </summary>
    /// <param name="channel">The channel to check (e.g., "8.0").</param>
    /// <param name="frameworkType">The framework type (default: ASP.NET Core).</param>
    /// <returns>An ApiResultBool containing a boolean indicating if the channel is installed.</returns>
    Task<ApiResultBool> IsChannelInstalledAsync(string channel, string frameworkType = DotNetFrameworkTypes.AspNetCore);

    /// <summary>
    /// Checks if a specific version is installed for a given framework type.
    /// </summary>
    /// <param name="version">The version to check (e.g., "8.0.1").</param>
    /// <param name="frameworkType">The framework type (default: ASP.NET Core).</param>
    /// <returns>An ApiResultBool containing a boolean indicating if the version is installed.</returns>
    Task<ApiResultBool> IsVersionInstalledAsync(string version, string frameworkType = DotNetFrameworkTypes.AspNetCore);

    /// <summary>
    /// Gets the list of available ASP.NET channels.
    /// </summary>
    /// <returns>A ChannelsResult containing a list of AspNetChannel objects representing available channels.</returns>
    Task<ChannelsResult> GetChannelsAsync();

    /// <summary>
    /// Gets the list of ASP.NET releases with installation status for a given channel.
    /// </summary>
    /// <param name="channel">The product version/channel to check (e.g., "8.0").</param>
    /// <returns>A ReleasesResult containing a list of AspNetRelease objects with installation status.</returns>
    Task<ReleasesResult> GetReleasesWithStatusAsync(string channel);
}

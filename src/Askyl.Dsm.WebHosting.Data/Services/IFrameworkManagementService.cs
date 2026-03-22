using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Data.Services;

/// <summary>
/// Facade service for framework (ASP.NET) installation and uninstallation operations.
/// </summary>
public interface IFrameworkManagementService
{
    /// <summary>
    /// Installs the specified ASP.NET framework version.
    /// </summary>
    /// <param name="version">The version to install (e.g., "8.0.1").</param>
    /// <param name="channel">The product channel (e.g., "8.0").</param>
    /// <returns>An InstallationResult indicating success or failure.</returns>
    Task<InstallationResult> InstallFrameworkAsync(string version, string channel);

    /// <summary>
    /// Uninstalls the specified ASP.NET framework version.
    /// </summary>
    /// <param name="version">The version to uninstall.</param>
    /// <returns>An InstallationResult indicating success or failure.</returns>
    Task<InstallationResult> UninstallFrameworkAsync(string version);
}

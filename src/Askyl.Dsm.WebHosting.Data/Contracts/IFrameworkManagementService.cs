using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An InstallationResult indicating success or failure.</returns>
    Task<InstallationResult> InstallFrameworkAsync(string version, string channel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls the specified ASP.NET framework version.
    /// </summary>
    /// <param name="version">The version to uninstall.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An InstallationResult indicating success or failure.</returns>
    Task<InstallationResult> UninstallFrameworkAsync(string version, CancellationToken cancellationToken = default);
}

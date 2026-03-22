using Askyl.Dsm.WebHosting.Data;

namespace Askyl.Dsm.WebHosting.Ui.Client.Interfaces;

/// <summary>
/// Facade service for license management operations.
/// Implemented by Ui.Client to fetch static license files from wwwroot/licenses/
/// via parallel HTTP requests for optimal performance.
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// Gets the list of available licenses, loading them on-demand if not already loaded.
    /// </summary>
    Task<IReadOnlyList<LicenseInfo>> GetLicensesAsync();
}

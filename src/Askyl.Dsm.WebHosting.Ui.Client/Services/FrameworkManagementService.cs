using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.API.Parameters;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Services;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Client-side proxy for IFrameworkManagementService that calls REST API endpoints.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
public class FrameworkManagementService(IHttpClientFactory httpClientFactory) : IFrameworkManagementService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<InstallationResult> InstallFrameworkAsync(string version, string channel)
    {
        var model = new InstallFrameworkModel(version, channel);
        return await _httpClient.PostJsonOrDefaultAsync<InstallFrameworkModel, InstallationResult>(FrameworkManagementRoutes.InstallFullRoute, model, () => InstallationResult.CreateFailure("Failed to install framework"));
    }

    /// <inheritdoc/>
    public async Task<InstallationResult> UninstallFrameworkAsync(string version)
        => await _httpClient.PostJsonOrDefaultAsync<object, InstallationResult>(FrameworkManagementRoutes.UninstallWithVersionFullRoute(version), new(), () => InstallationResult.CreateFailure($"Failed to uninstall framework version: {version}"));
}

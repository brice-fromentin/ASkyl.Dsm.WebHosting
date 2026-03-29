using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Contracts;
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
    public async Task<InstallationResult> InstallFrameworkAsync(string version, string channel, CancellationToken cancellationToken = default)
    {
        var model = new InstallFramework(version, channel);
        return await _httpClient.PostJsonOrDefaultAsync<InstallFramework, InstallationResult>(FrameworkManagementRoutes.InstallFullRoute, model, () => InstallationResult.CreateFailure("Failed to install framework"), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<InstallationResult> UninstallFrameworkAsync(string version, CancellationToken cancellationToken = default)
        => await _httpClient.PostJsonOrDefaultAsync<object, InstallationResult>(FrameworkManagementRoutes.UninstallWithVersionFullRoute(version), new(), () => InstallationResult.CreateFailure($"Failed to uninstall framework version: {version}"), cancellationToken);
}

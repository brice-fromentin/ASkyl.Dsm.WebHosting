using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.FileSystem;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Client-side proxy for IFileSystemService that calls REST API endpoints.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class FileSystemService(IHttpClientFactory httpClientFactory, ILocalizer localizer) : IFileSystemService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

    /// <inheritdoc/>
    public async Task<SharedFoldersResult> GetSharedFoldersAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetJsonOrDefaultAsync(FileManagementRoutes.SharedFoldersFullRoute, () => SharedFoldersResult.CreateFailure(localizer[LK.Error.FailedToLoadSharedFolders]), cancellationToken);

    /// <inheritdoc/>
    public async Task<DirectoryContentsResult> GetDirectoryContentsAsync(string path, bool directoryOnly, CancellationToken cancellationToken = default)
    {
        var url = $"{FileManagementRoutes.DirectoryContentsFullRoute}?path={Uri.EscapeDataString(path)}&directoryOnly={(directoryOnly ? "true" : "false")}";
        return await _httpClient.GetJsonOrDefaultAsync(url, () => DirectoryContentsResult.CreateFailure(localizer[LK.Error.FailedToLoadDirectoryContentsWithPath, path]), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<ApiResult> GrantSiteAccessAsync(SiteAccessRequest request, CancellationToken cancellationToken = default)
        => await _httpClient.PostJsonOrDefaultAsync(FileManagementRoutes.SiteAccessFullRoute, request, () => ApiResult.CreateFailure(localizer[LK.Error.SiteAccessGrantFailed, request.ApplicationPath]), cancellationToken);
}

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
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
    public async Task<SharedFoldersResult> GetSharedFoldersAsync()
        => await _httpClient.GetJsonOrDefaultAsync<SharedFoldersResult>(FileManagementRoutes.SharedFoldersFullRoute, () => SharedFoldersResult.CreateFailure(localizer[L.Error.FailedToLoadSharedFolders]));

    /// <inheritdoc/>
    public async Task<DirectoryContentsResult> GetDirectoryContentsAsync(string path, bool directoryOnly)
    {
        var parameters = new[] { ("path", path), ("directoryOnly", directoryOnly.ToLower()) };
        var url = FileManagementRoutes.DirectoryContentsFullRoute.WithQuery(parameters);
        return await _httpClient.GetJsonOrDefaultAsync<DirectoryContentsResult>(url, () => DirectoryContentsResult.CreateFailure(localizer[L.Error.FailedToLoadDirectoryContentsWithPath, path]));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method is now handled internally by WebSiteHostingService on the server side.
    /// It should not be called from the client - permissions are automatically set when adding/updating websites.
    /// </remarks>
    public Task<ApiResult> SetHttpGroupPermissionsAsync(string path, bool isDirectory)
        => throw new NotImplementedException("Permission setting is now handled internally by WebSiteHostingService. This method should not be called from the client.");
}

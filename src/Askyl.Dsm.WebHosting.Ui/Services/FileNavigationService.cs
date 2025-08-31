using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;
using Askyl.Dsm.WebHosting.Data.API.Responses;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Ui.Models;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface IFileNavigationService
{
    Task<List<DirectoryTreeNode>> GetSharedFoldersAsync(Func<string, Task> errorHandler);
    Task<List<FileStationFile>> GetDirectoryContentsAsync(string path);
    Task<List<DirectoryTreeNode>> GetDirectoryChildrenAsync(string path, Func<string, Task> errorHandler);
}

public class FileNavigationService(DsmApiClient apiClient, ILogger<FileNavigationService> logger) : IFileNavigationService
{
    private readonly DsmApiClient _apiClient = apiClient;
    private readonly ILogger<FileNavigationService> _logger = logger;

    public async Task<List<DirectoryTreeNode>> GetSharedFoldersAsync(Func<string, Task> errorHandler)
    {
        _logger.LogDebug("Retrieving shared folders from DSM FileStation API");

        var parameters = new FileStationListShareParameters(_apiClient.ApiInformations);
        var response = await _apiClient.ExecuteAsync<FileStationListShareResponse>(parameters);

        if (response?.Success != true || response.Data?.Shares == null)
        {
            _logger.LogError("FileStation API call failed: Success={Success}, ErrorCode={ErrorCode}", response?.Success, response?.Error?.Code);
            throw new FileStationApiException($"FileStation API call failed: Success={response?.Success}, ErrorCode={response?.Error?.Code}", response?.Success, response?.Error?.Code);
        }

        var sharedFolders = response.Data.Shares.Select(share => DirectoryTreeNode.CreateSharedFolder(share.Name, share.Path, this, errorHandler)).ToList();

        _logger.LogInformation("Retrieved {Count} shared folders", sharedFolders.Count);
        return sharedFolders;
    }

    public async Task<List<FileStationFile>> GetDirectoryContentsAsync(string path)
    {
        _logger.LogDebug("Retrieving directory contents for path: {Path}", path);

        var parameters = new FileStationListParameters(_apiClient.ApiInformations);
        parameters.Parameters.FolderPath = path;
        parameters.Parameters.Additional = "real_path,size,owner,time,perm,mount_point_type,type";
        parameters.Parameters.SortBy = "name";
        parameters.Parameters.SortDirection = "asc";

        var response = await _apiClient.ExecuteAsync<FileStationListResponse>(parameters);

        if (response?.Success != true || response.Data?.Files == null)
        {
            _logger.LogError("Failed to retrieve directory contents for {Path}: Success={Success}, ErrorCode={ErrorCode}", path, response?.Success, response?.Error?.Code);
            throw new FileStationApiException($"Failed to retrieve directory contents for {path}: Success={response?.Success}, ErrorCode={response?.Error?.Code}", response?.Success, response?.Error?.Code, path);
        }

        _logger.LogDebug("Retrieved {Count} files from directory {Path}", response.Data.Files.Count, path);
        return response.Data.Files;
    }

    public async Task<List<DirectoryTreeNode>> GetDirectoryChildrenAsync(string path, Func<string, Task> errorHandler)
    {
        _logger.LogDebug("Retrieving directory children for path: {Path}", path);

        var parameters = new FileStationListParameters(_apiClient.ApiInformations);
        parameters.Parameters.FolderPath = path;
        parameters.Parameters.SortBy = "name";
        parameters.Parameters.SortDirection = "asc";
        parameters.Parameters.FileType = "dir"; // Only directories

        var response = await _apiClient.ExecuteAsync<FileStationListResponse>(parameters);

        if (response?.Success != true || response.Data?.Files == null)
        {
            _logger.LogError("Failed to retrieve directory children for {Path}: Success={Success}, ErrorCode={ErrorCode}", path, response?.Success, response?.Error?.Code);
            throw new FileStationApiException($"Failed to retrieve directory children for {path}: Success={response?.Success}, ErrorCode={response?.Error?.Code}", response?.Success, response?.Error?.Code, path);
        }

        var children = response.Data.Files.Select(file => DirectoryTreeNode.FromFileStationFile(file, this, errorHandler)).ToList();

        _logger.LogDebug("Found {Count} child directories in {Path}", children.Count, path);
        return children;
    }
}

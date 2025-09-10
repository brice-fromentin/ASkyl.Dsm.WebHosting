using Askyl.Dsm.WebHosting.Constants.API;
using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Askyl.Dsm.WebHosting.Data.API.Parameters.FileStationAPI;
using Askyl.Dsm.WebHosting.Data.API.Responses;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Tools.Network;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Ui.Services;

public interface IFileNavigationService
{
    Task<List<TreeViewItem>> GetSharedFoldersAsync(Func<string, Task> errorHandler);
    Task<IQueryable<FileStationFile>> GetDirectoryContentsAsync(string path);
}

public class FileNavigationService(DsmApiClient apiClient, ILogger<FileNavigationService> logger) : IFileNavigationService
{

    #region Fields

    private readonly DsmApiClient _apiClient = apiClient;
    private readonly ILogger<FileNavigationService> _logger = logger;

    #endregion

    #region Public Methods

    public async Task<List<TreeViewItem>> GetSharedFoldersAsync(Func<string, Task> errorHandler)
    {
        _logger.LogDebug("Retrieving shared folders from DSM FileStation API");

        var parameters = new FileStationListShareParameters(_apiClient.ApiInformations);
        var response = await _apiClient.ExecuteAsync<FileStationListShareResponse>(parameters);

        if (response?.Success != true || response.Data?.Shares is null)
        {
            _logger.LogError("FileStation API call failed: Success={Success}, ErrorCode={ErrorCode}", response?.Success, response?.Error?.Code);
            throw new FileStationApiException($"FileStation API call failed: Success={response?.Success}, ErrorCode={response?.Error?.Code}", response?.Success, response?.Error?.Code);
        }

        var sharedFolders = response.Data.Shares.Select(share => CreateTreeViewItemWithLazyLoading(share.Path, share.Name, errorHandler)).ToList();

        _logger.LogInformation("Retrieved {Count} shared folders", sharedFolders.Count);
        return sharedFolders;
    }

    public async Task<IQueryable<FileStationFile>> GetDirectoryContentsAsync(string path)
    {
        _logger.LogDebug("Retrieving directory contents for path: {Path}", path);

        var directoriesTask = ExecuteFileStationListAsync(path, FileStationDefaults.PatternAll, FileStationDefaults.TypeDirectory);
        var filesTask = ExecuteFileStationListAsync(path, FileStationDefaults.PatternDllsExes, FileStationDefaults.TypeFile);

        var results = await Task.WhenAll(directoriesTask, filesTask);
        var directories = results[0];
        var files = results[1];

        var allContents = directories.Concat(files).ToList();

        _logger.LogDebug("Retrieved {DirectoryCount} directories and {FileCount} files from {Path}", directories.Count, files.Count, path);

        return allContents.AsQueryable();
    }

    #endregion

    #region Private Methods - API Operations

    private async Task<List<FileStationFile>> ExecuteFileStationListAsync(string path, string pattern, string fileType)
    {
        var parameters = new FileStationListParameters(_apiClient.ApiInformations);
        parameters.Parameters.FolderPath = path;
        parameters.Parameters.Additional = FileStationDefaults.AdditionalPathSizeTimeFields;
        parameters.Parameters.SortBy = FileStationDefaults.SortByName;
        parameters.Parameters.SortDirection = FileStationDefaults.SortDirectionAsc;

        parameters.Parameters.Pattern = !String.IsNullOrEmpty(pattern) ? pattern : FileStationDefaults.PatternAll;
        parameters.Parameters.FileType = !String.IsNullOrEmpty(fileType) ? fileType : FileStationDefaults.TypeAll;

        var response = await _apiClient.ExecuteAsync<FileStationListResponse>(parameters);

        if (response?.Success != true || response.Data?.Files is null)
        {
            _logger.LogError("Failed to retrieve directory contents for {Path}: Success={Success}, ErrorCode={ErrorCode}", path, response?.Success, response?.Error?.Code);
            throw new FileStationApiException($"Failed to retrieve directory contents for {path}: Success={response?.Success}, ErrorCode={response?.Error?.Code}", response?.Success, response?.Error?.Code, path);
        }

        return response.Data.Files;
    }

    #endregion

    #region Private Methods - TreeView Support

    private TreeViewItem CreateTreeViewItemWithLazyLoading(string path, string name, Func<string, Task> errorHandler)
        => new(path, name, TreeViewItem.LoadingTreeViewItems)
        {
            OnExpandedAsync = (args) => LoadChildDirectoriesAsync(args, errorHandler)
        };

    private async Task LoadChildDirectoriesAsync(TreeViewItemExpandedEventArgs args, Func<string, Task> errorHandler)
    {
        var path = args.CurrentItem.Id;

        _logger.LogDebug("Retrieving directory children for path: {Path}", path);

        var files = await ExecuteFileStationListAsync(path, FileStationDefaults.PatternAll, FileStationDefaults.TypeDirectory);
        var children = files.Select(file => CreateTreeViewItemWithLazyLoading(file.Path, file.Name, errorHandler)).ToList();

        _logger.LogDebug("Found {Count} child directories in {Path}", children.Count, path);

        args.CurrentItem.Expanded = args.Expanded;
        args.CurrentItem.Items = children;
    }

    #endregion

}
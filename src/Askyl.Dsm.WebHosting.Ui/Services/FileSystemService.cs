using Askyl.Dsm.WebHosting.Constants.DSM.FileStation;
using Askyl.Dsm.WebHosting.Data.Domain.FileSystem;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.CoreAcl;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Tools.Network;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Server-side implementation of IFileSystemService for Synology DSM FileStation API operations.
/// Returns simple FileSystemItem data objects; UI-specific rendering is handled by the client layer.
/// </summary>
public class FileSystemService(DsmApiClient apiClient, ILogger<FileSystemService> logger) : Data.Services.IFileSystemService
{
    private readonly DsmApiClient _apiClient = apiClient;
    private readonly ILogger<FileSystemService> _logger = logger;

    public async Task<SharedFoldersResult> GetSharedFoldersAsync()
    {
        _logger.LogDebug("Retrieving shared folders from DSM FileStation API");

        try
        {
            var sharedFolders = await ExecuteFileStationListShareAsync();

            _logger.LogInformation("Retrieved {Count} shared folders", sharedFolders.Count);

            return SharedFoldersResult.CreateSuccess([.. sharedFolders.Select(CreateFsEntry)]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shared folders");
            return SharedFoldersResult.CreateFailure($"Failed to retrieve shared folders: {ex.Message}");
        }
    }

    public async Task<DirectoryContentsResult> GetDirectoryContentsAsync(string path, bool directoryOnly)
    {
        _logger.LogDebug("Retrieving directory contents for path: {Path}, DirectoryOnly: {DirectoryOnly}", path, directoryOnly);

        try
        {
            if (directoryOnly)
            {
                // Single API call for directories only - more efficient
                var directoryFiles = await ExecuteFileStationListAsync(path, FileStationDefaults.PatternAll, FileStationDefaults.TypeDirectory); ;

                _logger.LogDebug("Retrieved {DirectoryCount} directories from {Path}", directoryFiles.Count, path);

                return DirectoryContentsResult.CreateSuccess([.. directoryFiles.Select(CreateFsEntry)]);
            }

            // Original behavior: both directories and files (for backward compatibility)
            var dirsTask = ExecuteFileStationListAsync(path, FileStationDefaults.PatternAll, FileStationDefaults.TypeDirectory);
            var filesTask = ExecuteFileStationListAsync(path, FileStationDefaults.PatternDllsExes, FileStationDefaults.TypeFile);

            var allResults = await Task.WhenAll(dirsTask, filesTask);
            var directories = allResults[0];
            var files = allResults[1];

            List<FsEntry> allContents = [.. directories.Concat(files).Select(CreateFsEntry)];

            _logger.LogDebug("Retrieved {DirectoryCount} directories and {FileCount} files from {Path}", directories.Count, files.Count, path);

            return DirectoryContentsResult.CreateSuccess(allContents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving directory contents for {Path}", path);
            return DirectoryContentsResult.CreateFailure($"Failed to retrieve directory contents: {ex.Message}");
        }
    }

    public async Task<ApiResult> SetHttpGroupPermissionsAsync(string path, bool isDirectory)
    {
        _logger.LogDebug("Setting HTTP group permissions for virtual path: {Path}", path);

        var targetPath = isDirectory ? path : Path.GetDirectoryName(path) ?? path;

        _logger.LogDebug("Target path: {TargetPath}, IsDirectory: {IsDirectory}", targetPath, isDirectory);

        var parameters = new CoreAclSetParameters(_apiClient.ApiInformations);
        parameters.Parameters.FilePath = targetPath;
        parameters.Parameters.Files = targetPath;
        parameters.Parameters.DirPaths = targetPath;
        parameters.Parameters.ChangeAcl = true;
        parameters.Parameters.Inherited = true;
        parameters.Parameters.AclRecur = false;

        parameters.Parameters.Rules =
        [
            new()
            {
                OwnerType = "group",
                OwnerName = "http",
                PermissionType = "allow",
                Permission = new()
                {
                    ReadData = true,
                    WriteData = true,
                    ExeFile = true,
                    AppendData = true,
                    Delete = true,
                    DeleteSub = true,
                    ReadAttr = true,
                    WriteAttr = true,
                    ReadExtAttr = true,
                    WriteExtAttr = true,
                    ReadPerm = true,
                    ChangePerm = false,
                    TakeOwnership = false
                },
                Inherit = new()
                {
                    ChildFiles = true,
                    ChildFolders = true,
                    ThisFolder = true,
                    AllDescendants = true
                }
            }
        ];

        var response = await _apiClient.ExecuteAsync<CoreAclSetResponse>(parameters);

        if (response?.Success != true || response.Data?.TaskId is null)
        {
            _logger.LogError("Failed to set ACL permissions for {Path}: Success={Success}, ErrorCode={ErrorCode}", path, response?.Success, response?.Error?.Code);
            return ApiResult.CreateFailure($"Failed to set ACL permissions for {path}: Success={response?.Success}, ErrorCode={response?.Error?.Code}");
        }

        _logger.LogInformation("ACL permissions set successfully for {Path}, TaskId: {TaskId}", path, response.Data.TaskId);
        return ApiResult.CreateSuccess();
    }

    private async Task<List<FileStationShare>> ExecuteFileStationListShareAsync()
    {
        var parameters = new FileStationListShareParameters(_apiClient.ApiInformations);
        parameters.Parameters.Additional = FileStationDefaults.AdditionalPathSizeTimeFields;
        parameters.Parameters.SortBy = FileStationDefaults.SortByName;
        parameters.Parameters.SortDirection = FileStationDefaults.SortDirectionAsc;

        var response = await _apiClient.ExecuteAsync<FileStationListShareResponse>(parameters);

        if (response?.Success != true || response.Data?.Shares is null)
        {
            _logger.LogError("FileStation API call failed: Success={Success}, ErrorCode={ErrorCode}", response?.Success, response?.Error?.Code);
            throw new FileStationApiException($"FileStation API call failed: Success={response?.Success}, ErrorCode={response?.Error?.Code}", response?.Success, response?.Error?.Code);
        }

        return response.Data.Shares;
    }

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

    /// <summary>
    /// Creates an FsEntry from a FileStationFile.
    /// Note: All API calls request AdditionalPathSizeTimeFields, so Additional is guaranteed to be populated.
    /// </summary>
    private static FsEntry CreateFsEntry(FileStationFile file)
        => new(file.Path, file.Name, file.IsDirectory, file.Additional!.RealPath!, Size: file.IsDirectory ? null : file.Additional!.Size!, Modified: DateTimeOffset.FromUnixTimeSeconds(file.Additional!.Time!.ModifyTime!.Value).UtcDateTime);

    /// <summary>
    /// Creates an FsEntry from a FileStationShare.
    /// Note: All API calls request AdditionalPathSizeTimeFields, so Additional is guaranteed to be populated.
    /// </summary>
    private static FsEntry CreateFsEntry(FileStationShare share)
        => new(share.Path, share.Name, share.IsDirectory, share.Additional!.RealPath!, Size: null, Modified: DateTimeOffset.FromUnixTimeSeconds(share.Additional!.Time!.ModifyTime!.Value).UtcDateTime);
}

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Constants.DSM.FileStation;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.FileSystem;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.Acl;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.FileStation;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.Acl;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.FileStation;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Server-side implementation of IFileSystemService for Synology DSM FileStation API operations.
/// Returns simple FileSystemItem data objects; UI-specific rendering is handled by the client layer.
/// </summary>
public class FileSystemService(IDsmSession dsmSession, ILogger<ILogFileSystemService> logger, ILocalizer localizer) : Data.Contracts.IFileSystemService
{
    public async Task<SharedFoldersResult> GetSharedFoldersAsync(CancellationToken cancellationToken = default)
    {
        logger.RetrievingSharedFolders();

        try
        {
            var sharedFolders = await ExecuteFileStationListShareAsync(cancellationToken);

            logger.RetrievedSharedFolders(sharedFolders.Count);

            return SharedFoldersResult.CreateSuccess([.. sharedFolders.Select(CreateFsEntry)]);
        }
        catch (Exception ex)
        {
            logger.ErrorRetrievingSharedFolders(ex);
            return SharedFoldersResult.CreateFailure(localizer[LK.Error.OperationFailed]);
        }
    }

    public async Task<DirectoryContentsResult> GetDirectoryContentsAsync(string path, bool directoryOnly, CancellationToken cancellationToken = default)
    {
        // Validate path to prevent path traversal attacks
        if (!IsPathValid(path))
        {
            logger.PathValidationFailed(path);
            return DirectoryContentsResult.CreateFailure(localizer[LK.Validation.PathTraversalDetected]);
        }

        logger.RetrievingDirectoryContents(path, directoryOnly);

        try
        {
            if (directoryOnly)
            {
                // Single API call for directories only - more efficient
                var directoryFiles = await ExecuteFileStationListAsync(path, FileStationDefaults.PatternAll, FileStationDefaults.TypeDirectory, cancellationToken);

                logger.RetrievedDirectories(directoryFiles.Count, path);

                return DirectoryContentsResult.CreateSuccess([.. directoryFiles.Select(CreateFsEntry)]);
            }

            // Original behavior: both directories and files (for backward compatibility)
            var dirsTask = ExecuteFileStationListAsync(path, FileStationDefaults.PatternAll, FileStationDefaults.TypeDirectory, cancellationToken);
            var filesTask = ExecuteFileStationListAsync(path, FileStationDefaults.PatternDllsExes, FileStationDefaults.TypeFile, cancellationToken);

            var allResults = await Task.WhenAll(dirsTask, filesTask);
            var directories = allResults[0];
            var files = allResults[1];

            List<FsEntry> allContents = [.. directories.Concat(files).Select(CreateFsEntry)];

            logger.RetrievedDirectoriesAndFiles(directories.Count, files.Count, path);

            return DirectoryContentsResult.CreateSuccess(allContents);
        }
        catch (Exception ex)
        {
            logger.ErrorRetrievingDirectory(ex, path);
            return DirectoryContentsResult.CreateFailure(localizer[LK.Error.OperationFailed]);
        }
    }

    public async Task<ApiResult> SetHttpGroupPermissionsAsync(string path, bool isDirectory, CancellationToken cancellationToken = default)
    {
        logger.SettingHttpGroupPermissions(path);

        // Validate path to prevent path traversal attacks
        if (!IsPathValid(path))
        {
            logger.PathValidationFailed(path);
            return ApiResult.CreateFailure(localizer[LK.Validation.PathTraversalDetected]);
        }

        var targetPath = isDirectory ? path : Path.GetDirectoryName(path) ?? path;

        logger.TargetPathInfo(targetPath, isDirectory);

        var aclSet = new CoreAclSet
        {
            FilePath = targetPath,
            Files = targetPath,
            DirPaths = targetPath,
            ChangeAcl = true,
            Inherited = true,
            AclRecur = false,
            Rules =
            [
                new()
                {
                    OwnerType = ReverseProxyConstants.AclOwnerTypeGroup,
                    OwnerName = ReverseProxyConstants.AclOwnerNameHttp,
                    PermissionType = ReverseProxyConstants.AclPermissionTypeAllow,
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
            ]
        };

        var parameters = new CoreAclSetParameters(aclSet);

        var response = await dsmSession.ExecuteAsync<CoreAclSetResponse>(parameters, cancellationToken);

        if (response?.Success != true || response.Data?.TaskId is null)
        {
            logger.FailedToSetAclPermissions(path, response?.Success, response?.Error?.Code);
            return ApiResult.CreateFailure(localizer[LK.Error.FailedToSetACL, path, response?.Success ?? false, response?.Error?.Code ?? 0]);
        }

        logger.AclPermissionsSet(path, response.Data.TaskId);
        return ApiResult.CreateSuccess();
    }

    private async Task<List<FileStationShare>> ExecuteFileStationListShareAsync(CancellationToken cancellationToken)
    {
        var entry = new FileStationListShare
        {
            Additional = FileStationDefaults.AdditionalPathSizeTimeFields,
            SortBy = FileStationDefaults.SortByName,
            SortDirection = FileStationDefaults.SortDirectionAsc
        };

        var parameters = new FileStationListShareParameters(entry);

        var response = await dsmSession.ExecuteAsync<FileStationListShareResponse>(parameters, cancellationToken);

        if (response?.Success != true || response.Data?.Shares is null)
        {
            throw new FileStationApiException($"FileStation list share operation failed: Success={response?.Success}, ErrorCode={response?.Error?.Code}", response?.Success, response?.Error?.Code);
        }

        return response.Data.Shares;
    }

    private async Task<List<FileStationFile>> ExecuteFileStationListAsync(string path, string pattern, string fileType, CancellationToken cancellationToken)
    {
        var entry = new FileStationList
        {
            FolderPath = path,
            Additional = FileStationDefaults.AdditionalPathSizeTimeFields,
            SortBy = FileStationDefaults.SortByName,
            SortDirection = FileStationDefaults.SortDirectionAsc,
            Pattern = !String.IsNullOrEmpty(pattern) ? pattern : FileStationDefaults.PatternAll,
            FileType = !String.IsNullOrEmpty(fileType) ? fileType : FileStationDefaults.TypeAll
        };

        var parameters = new FileStationListParameters(entry);

        var response = await dsmSession.ExecuteAsync<FileStationListResponse>(parameters, cancellationToken);

        if (response?.Success != true || response.Data?.Files is null)
        {
            throw new FileStationApiException($"Failed to retrieve directory contents for {path}: Success={response?.Success}, ErrorCode={response?.Error?.Code}", response?.Success, response?.Error?.Code, path);
        }

        return response.Data.Files;
    }

    /// <summary>
    /// Creates an FsEntry from a FileStationFile.
    /// Note: All API calls request AdditionalPathSizeTimeFields, so Additional is guaranteed to be populated.
    /// </summary>
    private static FsEntry CreateFsEntry(FileStationFile file)
        => new(file.Path, file.Name, file.IsDirectory, file.Additional!.RealPath!, Size: file.IsDirectory ? null : file.Additional!.Size!, Modified: DateTimeOffset.FromUnixTimeSeconds(file.Additional!.Time!.ModifyTime!.Value).LocalDateTime);

    /// <summary>
    /// Creates an FsEntry from a FileStationShare.
    /// Note: All API calls request AdditionalPathSizeTimeFields, so Additional is guaranteed to be populated.
    /// </summary>
    private static FsEntry CreateFsEntry(FileStationShare share)
        => new(share.Path, share.Name, share.IsDirectory, share.Additional!.RealPath!, Size: null, Modified: DateTimeOffset.FromUnixTimeSeconds(share.Additional!.Time!.ModifyTime!.Value).LocalDateTime);

    /// <summary>
    /// Validates a DSM virtual path to prevent path traversal attacks.
    /// </summary>
    private static bool IsPathValid(string path)
    {
        if (String.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Check for literal path traversal
        if (path.Contains(ValidationConstants.PathTraversalLiteral))
        {
            return false;
        }

        // Check for URL-encoded path traversal (%2e = '.', %2f = '/')
        var lowerPath = path.ToLowerInvariant();
        return !lowerPath.Contains(ValidationConstants.PathTraversalEncodedDot) && !lowerPath.Contains(ValidationConstants.PathTraversalEncodedSlash);
    }
}

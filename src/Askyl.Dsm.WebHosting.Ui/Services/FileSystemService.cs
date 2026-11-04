using System.Diagnostics;
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.ErrorRetrievingDirectory(ex, path);
            return DirectoryContentsResult.CreateFailure(localizer[LK.Error.OperationFailed]);
        }
    }

    public async Task<ApiResult> GrantSiteAccessAsync(SiteAccessRequest request, CancellationToken cancellationToken = default)
    {
        var applicationPath = request.ApplicationPath;
        var realPath = request.ApplicationRealPath;

        // Both come from the browser: the share path must be the real path's own tail, so only the volume differs
        if (!IsPathValid(applicationPath) || !IsPathValid(realPath)
            || applicationPath[0] != FileStationDefaults.PathSeparator || !realPath.EndsWith(applicationPath, StringComparison.Ordinal))
        {
            logger.PathValidationFailed(applicationPath);
            return ApiResult.CreateFailure(localizer[LK.Validation.PathTraversalDetected]);
        }

        var volume = realPath[..^applicationPath.Length];
        var folders = applicationPath.Split(FileStationDefaults.PathSeparator, StringSplitOptions.RemoveEmptyEntries)[..^1];

        if (folders.Length == 0)
        {
            logger.PathValidationFailed(applicationPath);
            return ApiResult.CreateFailure(localizer[LK.Validation.PathTraversalDetected]);
        }

        try
        {
            // The shared folder first, the site's own folder last: each folder above it is passed through, never read
            for (var depth = 1; depth <= folders.Length; depth++)
            {
                var sharePath = FileStationDefaults.PathSeparator + String.Join(FileStationDefaults.PathSeparator, folders[..depth]);

                if (!await GrantHttpAccessAsync(volume + sharePath, sharePath, depth == folders.Length, cancellationToken))
                {
                    return ApiResult.CreateFailure(localizer[LK.Error.SiteAccessGrantFailed, applicationPath]);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.SiteAccessGrantError(ex, applicationPath);
            return ApiResult.CreateFailure(localizer[LK.Error.SiteAccessGrantFailed, applicationPath]);
        }

        return ApiResult.CreateSuccess();
    }

    private static readonly CoreAclRule HttpTraverseRule = new()
    {
        OwnerType = AclConstants.OwnerTypeGroup,
        OwnerName = AclConstants.OwnerNameHttp,
        PermissionType = AclConstants.PermissionTypeAllow,
        Permission = new() { ExeFile = true },
        Inherit = new() { ThisFolder = true }
    };

    private static readonly CoreAclRule HttpReadRule = HttpTraverseRule with
    {
        Permission = new() { ReadData = true, ExeFile = true, ReadAttr = true, ReadExtAttr = true, ReadPerm = true },
        Inherit = new() { ThisFolder = true, ChildFiles = true, ChildFolders = true, AllDescendants = true }
    };

    /// <summary>
    /// Adds one http entry to a folder the way DSM's own permission editor does: read the ACL, send every explicit
    /// entry back with the new one (<c>set</c> replaces them all), then wait for the task DSM answers with.
    /// </summary>
    private async Task<bool> GrantHttpAccessAsync(string realPath, string sharePath, bool isSiteFolder, CancellationToken cancellationToken)
    {
        var current = await dsmSession.ExecuteAsync<CoreAclGetResponse>(new CoreAclGetParameters(new() { FilePath = realPath, Type = AclConstants.GetTypeAll, IncludeNonameRules = true }), cancellationToken);

        if (current?.Success != true || current.Data is null)
        {
            logger.SiteAccessRefused(sharePath, ApiConstants.MethodGet, current?.Error?.Code);
            return false;
        }

        List<CoreAclRule> explicitRules = [.. current.Data.Acl.Where(r => r.Level == AclConstants.ExplicitLevel).Select(r => r with { Level = null })];

        // An http entry someone set is theirs: replacing it could take away what they gave, or lift what they denied
        if (explicitRules.Any(r => r.OwnerType == AclConstants.OwnerTypeGroup && r.OwnerName == AclConstants.OwnerNameHttp))
        {
            logger.SiteAccessLeftAlone(sharePath);
            return true;
        }

        var aclSet = new CoreAclSet
        {
            FilePath = realPath,
            Files = realPath,
            DirPaths = sharePath,
            ChangeAcl = true,
            Inherited = current.Data.IsInherited,
            AclRecur = false,
            Rules = [.. explicitRules, isSiteFolder ? HttpReadRule : HttpTraverseRule]
        };

        var response = await dsmSession.ExecuteAsync<CoreAclSetResponse>(new CoreAclSetParameters(aclSet), cancellationToken);

        if (response?.Success != true || String.IsNullOrEmpty(response.Data?.TaskId))
        {
            logger.SiteAccessRefused(sharePath, ApiConstants.MethodSet, response?.Error?.Code);
            return false;
        }

        if (!await WaitForAclTaskAsync(response.Data.TaskId, cancellationToken))
        {
            logger.AclTaskTimedOut(sharePath, AclConstants.TaskTimeoutSeconds);
            return false;
        }

        logger.SiteAccessGranted(sharePath, isSiteFolder);
        return true;
    }

    /// <summary>
    /// Polls a <c>set</c> task until DSM reports it finished, then releases it as DSM's editor does.
    /// </summary>
    private async Task<bool> WaitForAclTaskAsync(string taskId, CancellationToken cancellationToken)
    {
        var task = new CoreAclTask { TaskId = taskId };
        var started = Stopwatch.GetTimestamp();
        var finished = false;

        while (!finished && Stopwatch.GetElapsedTime(started).TotalSeconds < AclConstants.TaskTimeoutSeconds)
        {
            var status = await dsmSession.ExecuteAsync<CoreAclStatusResponse>(new CoreAclStatusParameters(task), cancellationToken);
            finished = status?.Data?.Finished == true;

            if (!finished)
            {
                await Task.Delay(AclConstants.TaskPollIntervalMilliseconds, cancellationToken);
            }
        }

        try
        {
            await dsmSession.ExecuteSimpleAsync(new CoreAclStopParameters(task), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Releasing the task is housekeeping: the ACL is already applied, or never will be
            logger.AclTaskReleaseFailed(ex, taskId);
        }

        return finished;
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

        if (lowerPath.Contains(ValidationConstants.PathTraversalEncodedDot) || lowerPath.Contains(ValidationConstants.PathTraversalEncodedSlash))
        {
            return false;
        }

        // Check for double-encoded path traversal (%252e decodes to %2e -> '.', %252f decodes to %2f -> '/')
        if (lowerPath.Contains(ValidationConstants.PathTraversalDoubleEncodedDot) || lowerPath.Contains(ValidationConstants.PathTraversalDoubleEncodedSlash))
        {
            return false;
        }

        return true;
    }
}

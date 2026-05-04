using Askyl.Dsm.WebHosting.Data.Domain.FileSystem;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Tests.Data.Results;

public class ResultTypesTests
{
    #region ApiResult

    [Fact]
    public void ApiResult_CreateSuccess_SetsSuccessAndNoError()
    {
        // Act
        var result = ApiResult.CreateSuccess("Done");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Done", result.Message);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void ApiResult_CreateSuccess_WithoutMessage_NullMessage()
    {
        // Act
        var result = ApiResult.CreateSuccess();

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Message);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void ApiResult_CreateFailure_WithMessage_SetsFailureAndDefaultCode()
    {
        // Act
        var result = ApiResult.CreateFailure("Oops");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Oops", result.Message);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void ApiResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = ApiResult.CreateFailure(ApiErrorCode.NotFound, "Missing");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Missing", result.Message);
        Assert.Equal(ApiErrorCode.NotFound, result.ErrorCode);
    }

    #endregion

    #region ApiResultBool

    [Fact]
    public void ApiResultBool_CreateSuccess_SetsValueAndSuccess()
    {
        // Act
        var result = ApiResultBool.CreateSuccess(true, "OK");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(true, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
        Assert.Equal("OK", result.Message);
    }

    [Fact]
    public void ApiResultBool_CreateSuccess_FalseValue()
    {
        // Act
        var result = ApiResultBool.CreateSuccess(false);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(false, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void ApiResultBool_CreateFailure_SetsFalseAndFailureCode()
    {
        // Act
        var result = ApiResultBool.CreateFailure("Nope");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(false, result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void ApiResultBool_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = ApiResultBool.CreateFailure(ApiErrorCode.Unauthorized, "Denied");

        // Assert
        Assert.False(result.Success);
        Assert.Equal(false, result.Value);
        Assert.Equal(ApiErrorCode.Unauthorized, result.ErrorCode);
    }

    #endregion

    #region AuthenticationResult

    [Fact]
    public void AuthenticationResult_CreateAuthenticated_SetsSuccess()
    {
        // Act
        var result = AuthenticationResult.CreateAuthenticated("Welcome");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IsAuthenticated);
        Assert.Equal("Welcome", result.Message);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void AuthenticationResult_CreateNotAuthenticated_SetsFailure()
    {
        // Act
        var result = AuthenticationResult.CreateNotAuthenticated("Bad credentials");

        // Assert
        Assert.False(result.Success);
        Assert.False(result.IsAuthenticated);
        Assert.Equal("Bad credentials", result.Message);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void AuthenticationResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = AuthenticationResult.CreateFailure(ApiErrorCode.Unauthorized, "Locked out");

        // Assert
        Assert.False(result.Success);
        Assert.False(result.IsAuthenticated);
        Assert.Equal(ApiErrorCode.Unauthorized, result.ErrorCode);
    }

    #endregion

    #region InstallationResult

    [Fact]
    public void InstallationResult_CreateSuccess_SetsVersionAndInstalledAt()
    {
        // Act
        var result = InstallationResult.CreateSuccess("8.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("8.0.1", result.Version);
        Assert.NotNull(result.InstalledAt);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void InstallationResult_CreateFailure_SetsNullVersionAndNoDate()
    {
        // Act
        var result = InstallationResult.CreateFailure("Network error");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Version);
        Assert.Null(result.InstalledAt);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void InstallationResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = InstallationResult.CreateFailure(ApiErrorCode.InvalidState, "Already installed");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Version);
        Assert.Equal(ApiErrorCode.InvalidState, result.ErrorCode);
    }

    #endregion

    #region SharedFoldersResult (ApiResultItems<FsEntry>)

    [Fact]
    public void SharedFoldersResult_CreateSuccess_SetsValueAndSuccess()
    {
        // Arrange
        var entries = new List<FsEntry> { new("/path/folder1", "folder1", true, "/real/folder1", null, DateTime.UtcNow) };

        // Act
        var result = SharedFoldersResult.CreateSuccess(entries, "OK");

        // Assert
        Assert.True(result.Success);
        Assert.Same(entries, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
        Assert.Equal("OK", result.Message);
    }

    [Fact]
    public void SharedFoldersResult_CreateFailure_SetsNullAndFailureCode()
    {
        // Act
        var result = SharedFoldersResult.CreateFailure("Disk error");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void SharedFoldersResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = SharedFoldersResult.CreateFailure(ApiErrorCode.NotFound, "Missing");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.NotFound, result.ErrorCode);
    }

    #endregion

    #region DirectoryContentsResult

    [Fact]
    public void DirectoryContentsResult_CreateSuccess_SetsValueAndSuccess()
    {
        // Arrange
        var entries = new List<FsEntry> { new("/path/subdir", "subdir", true, "/real/subdir", null, DateTime.UtcNow) };

        // Act
        var result = DirectoryContentsResult.CreateSuccess(entries);

        // Assert
        Assert.True(result.Success);
        Assert.Same(entries, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void DirectoryContentsResult_CreateFailure_SetsNullAndFailureCode()
    {
        // Act
        var result = DirectoryContentsResult.CreateFailure("Permission denied");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void DirectoryContentsResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = DirectoryContentsResult.CreateFailure(ApiErrorCode.BadRequest, "Invalid path");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.BadRequest, result.ErrorCode);
    }

    #endregion

    #region DirectoryFilesResult

    [Fact]
    public void DirectoryFilesResult_CreateSuccess_SetsValueAndSuccess()
    {
        // Arrange
        var entries = new List<FsEntry> { new("/path/file.txt", "file.txt", false, "/real/file.txt", 1024, DateTime.UtcNow) };

        // Act
        var result = DirectoryFilesResult.CreateSuccess(entries);

        // Assert
        Assert.True(result.Success);
        Assert.Same(entries, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void DirectoryFilesResult_CreateFailure_SetsNullAndFailureCode()
    {
        // Act
        var result = DirectoryFilesResult.CreateFailure("IO error");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void DirectoryFilesResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = DirectoryFilesResult.CreateFailure(ApiErrorCode.NotFound, "Gone");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.NotFound, result.ErrorCode);
    }

    #endregion

    #region ChannelsResult

    [Fact]
    public void ChannelsResult_CreateSuccess_SetsValueAndSuccess()
    {
        // Arrange
        var channels = new List<AspNetChannel> { new(CreateReleaseInfo("8.0.1", "8.0", isLts: true)) };

        // Act
        var result = ChannelsResult.CreateSuccess(channels, "Fetched");

        // Assert
        Assert.True(result.Success);
        Assert.Same(channels, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void ChannelsResult_CreateFailure_SetsNullAndFailureCode()
    {
        // Act
        var result = ChannelsResult.CreateFailure("Network error");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void ChannelsResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = ChannelsResult.CreateFailure(ApiErrorCode.Failure, "Timeout");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    #endregion

    #region ReleasesResult

    [Fact]
    public void ReleasesResult_CreateSuccess_SetsValueAndSuccess()
    {
        // Arrange
        var releases = new List<AspNetRelease> { AspNetRelease.Create(CreateReleaseInfo("8.0.1", "8.0")) };

        // Act
        var result = ReleasesResult.CreateSuccess(releases);

        // Assert
        Assert.True(result.Success);
        Assert.Same(releases, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void ReleasesResult_CreateFailure_SetsNullAndFailureCode()
    {
        // Act
        var result = ReleasesResult.CreateFailure("Fetch failed");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void ReleasesResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = ReleasesResult.CreateFailure(ApiErrorCode.NotFound, "Missing");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.NotFound, result.ErrorCode);
    }

    #endregion

    #region InstalledVersionsResult

    [Fact]
    public void InstalledVersionsResult_CreateSuccess_SetsValueAndSuccess()
    {
        // Arrange
        var versions = new List<FrameworkInfo> { new("Microsoft.NETCore.App", "8.0.1") };

        // Act
        var result = InstalledVersionsResult.CreateSuccess(versions);

        // Assert
        Assert.True(result.Success);
        Assert.Same(versions, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void InstalledVersionsResult_CreateFailure_SetsNullAndFailureCode()
    {
        // Act
        var result = InstalledVersionsResult.CreateFailure("Parse failed");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void InstalledVersionsResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = InstalledVersionsResult.CreateFailure(ApiErrorCode.Failure, "Timeout");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    #endregion

    #region WebSiteInstanceResult

    [Fact]
    public void WebSiteInstanceResult_CreateSuccess_SetsValueAndSuccess()
    {
        // Arrange
        var instance = new WebSiteInstance(new WebSiteConfiguration { Name = "Test" });

        // Act
        var result = WebSiteInstanceResult.CreateSuccess(instance, "Added");

        // Assert
        Assert.True(result.Success);
        Assert.Same(instance, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
        Assert.Equal("Added", result.Message);
    }

    [Fact]
    public void WebSiteInstanceResult_CreateFailure_SetsNullAndFailureCode()
    {
        // Act
        var result = WebSiteInstanceResult.CreateFailure("Config error");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void WebSiteInstanceResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = WebSiteInstanceResult.CreateFailure(ApiErrorCode.InvalidState, "Conflict");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.InvalidState, result.ErrorCode);
    }

    #endregion

    #region WebSiteInstancesResult

    [Fact]
    public void WebSiteInstancesResult_CreateSuccess_SetsValueAndSuccess()
    {
        // Arrange
        var instances = new List<WebSiteInstance> { new(new WebSiteConfiguration { Name = "Test" }) };

        // Act
        var result = WebSiteInstancesResult.CreateSuccess(instances);

        // Assert
        Assert.True(result.Success);
        Assert.Same(instances, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void WebSiteInstancesResult_CreateFailure_SetsNullAndFailureCode()
    {
        // Act
        var result = WebSiteInstancesResult.CreateFailure("Load failed");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void WebSiteInstancesResult_CreateFailure_WithErrorCode_SetsCustomCode()
    {
        // Act
        var result = WebSiteInstancesResult.CreateFailure(ApiErrorCode.NotFound, "Empty");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.NotFound, result.ErrorCode);
    }

    #endregion

    #region Helpers

    private static AspNetCoreReleaseInfo CreateReleaseInfo(
        string version,
        string productVersion,
        bool isLts = false,
        bool isSecurity = false,
        AspNetCoreReleaseType releaseType = AspNetCoreReleaseType.Unknown)
    {
        return new(version, productVersion, DateTimeOffset.UtcNow, isSecurity, isLts, releaseType);
    }

    #endregion
}

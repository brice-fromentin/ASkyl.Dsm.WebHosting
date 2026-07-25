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

    #endregion

    #region AuthenticationResult

    [Fact]
    public void AuthenticationResult_CreateAuthenticated_SetsSuccess()
    {
        // Act
        var result = AuthenticationResult.CreateAuthenticated("Welcome", "en-US");

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

    #endregion

    #region Items Result Types

    [Fact]
    public void SharedFoldersResult_CreateSuccess_SetsValueAndSuccess()
    {
        var items = new List<FsEntry> { new("/path/folder1", "folder1", true, "/real/folder1", null, DateTime.UtcNow) };
        var result = SharedFoldersResult.CreateSuccess(items, "OK");

        Assert.True(result.Success);
        Assert.Same(items, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void DirectoryContentsResult_CreateSuccess_SetsValueAndSuccess()
    {
        var items = new List<FsEntry> { new("/path/subdir", "subdir", true, "/real/subdir", null, DateTime.UtcNow) };
        var result = DirectoryContentsResult.CreateSuccess(items);

        Assert.True(result.Success);
        Assert.Same(items, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void ChannelsResult_CreateSuccess_SetsValueAndSuccess()
    {
        var items = new List<AspNetCoreReleaseInfo> { CreateReleaseInfo("8.0.1", "8.0", isLts: true) };
        var result = ChannelsResult.CreateSuccess(items, "OK");

        Assert.True(result.Success);
        Assert.Same(items, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void ReleasesResult_CreateSuccess_SetsValueAndSuccess()
    {
        var items = new List<AspNetRelease> { AspNetRelease.Create(CreateReleaseInfo("8.0.1", "8.0")) };
        var result = ReleasesResult.CreateSuccess(items);

        Assert.True(result.Success);
        Assert.Same(items, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void InstalledVersionsResult_CreateSuccess_SetsValueAndSuccess()
    {
        var items = new List<FrameworkInfo> { new("Microsoft.NETCore.App", "8.0.1") };
        var result = InstalledVersionsResult.CreateSuccess(items);

        Assert.True(result.Success);
        Assert.Same(items, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void WebSiteInstancesResult_CreateSuccess_SetsValueAndSuccess()
    {
        var items = new List<WebSiteInstance> { new(new WebSiteConfiguration { Name = "Test" }) };
        var result = WebSiteInstancesResult.CreateSuccess(items);

        Assert.True(result.Success);
        Assert.Same(items, result.Value);
        Assert.Equal(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public void SharedFoldersResult_CreateFailure_SetsExpectedProperties()
    {
        var result = SharedFoldersResult.CreateFailure("Disk error");
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void DirectoryContentsResult_CreateFailure_SetsExpectedProperties()
    {
        var result = DirectoryContentsResult.CreateFailure("Permission denied");
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void ChannelsResult_CreateFailure_SetsExpectedProperties()
    {
        var result = ChannelsResult.CreateFailure("Network error");
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void ReleasesResult_CreateFailure_SetsExpectedProperties()
    {
        var result = ReleasesResult.CreateFailure("Fetch failed");
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void InstalledVersionsResult_CreateFailure_SetsExpectedProperties()
    {
        var result = InstalledVersionsResult.CreateFailure("Parse failed");
        Assert.False(result.Success);
        Assert.Null(result.Value);
        Assert.Equal(ApiErrorCode.Failure, result.ErrorCode);
    }

    [Fact]
    public void WebSiteInstancesResult_CreateFailure_SetsExpectedProperties()
    {
        var result = WebSiteInstancesResult.CreateFailure("Load failed");
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

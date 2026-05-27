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

    #region Items Result Types - Parameterized

    public static IEnumerable<object[]> GetItemsSuccessTestData()
    {
        var sharedFolders = new List<FsEntry> { new("/path/folder1", "folder1", true, "/real/folder1", null, DateTime.UtcNow) };
        yield return new object[] { "SharedFoldersResult", sharedFolders, (Func<object, string, object>)((v, m) => SharedFoldersResult.CreateSuccess((List<FsEntry>)v, m)) };

        var directoryContents = new List<FsEntry> { new("/path/subdir", "subdir", true, "/real/subdir", null, DateTime.UtcNow) };
        yield return new object[] { "DirectoryContentsResult", directoryContents, (Func<object, string, object>)((v, _) => DirectoryContentsResult.CreateSuccess((List<FsEntry>)v)) };

        var channels = new List<AspNetChannel> { new(CreateReleaseInfo("8.0.1", "8.0", isLts: true)) };
        yield return new object[] { "ChannelsResult", channels, (Func<object, string, object>)((v, m) => ChannelsResult.CreateSuccess((List<AspNetChannel>)v, m)) };

        var releases = new List<AspNetRelease> { AspNetRelease.Create(CreateReleaseInfo("8.0.1", "8.0")) };
        yield return new object[] { "ReleasesResult", releases, (Func<object, string, object>)((v, _) => ReleasesResult.CreateSuccess((List<AspNetRelease>)v)) };

        var versions = new List<FrameworkInfo> { new("Microsoft.NETCore.App", "8.0.1") };
        yield return new object[] { "InstalledVersionsResult", versions, (Func<object, string, object>)((v, _) => InstalledVersionsResult.CreateSuccess((List<FrameworkInfo>)v)) };

        var instances = new List<WebSiteInstance> { new(new WebSiteConfiguration { Name = "Test" }) };
        yield return new object[] { "WebSiteInstancesResult", instances, (Func<object, string, object>)((v, _) => WebSiteInstancesResult.CreateSuccess((List<WebSiteInstance>)v)) };
    }

    [Theory]
    [MemberData(nameof(GetItemsSuccessTestData))]
    public void ItemsResults_CreateSuccess_SetsValueAndSuccess(string _, object value, Func<object, string, object> createSuccess)
    {
        // Act
        var result = createSuccess(value, "OK");

        // Assert
        Assert.True(((dynamic)result).Success);
        Assert.Same(value, ((dynamic)result).Value);
        Assert.Equal(ApiErrorCode.None, ((dynamic)result).ErrorCode);
    }

    public static IEnumerable<object[]> GetItemsFailureTestData()
    {
        yield return new object[] { "SharedFoldersResult", SharedFoldersResult.CreateFailure("Disk error") };
        yield return new object[] { "DirectoryContentsResult", DirectoryContentsResult.CreateFailure("Permission denied") };
        yield return new object[] { "ChannelsResult", ChannelsResult.CreateFailure("Network error") };
        yield return new object[] { "ReleasesResult", ReleasesResult.CreateFailure("Fetch failed") };
        yield return new object[] { "InstalledVersionsResult", InstalledVersionsResult.CreateFailure("Parse failed") };
        yield return new object[] { "WebSiteInstancesResult", WebSiteInstancesResult.CreateFailure("Load failed") };
    }

    [Theory]
    [MemberData(nameof(GetItemsFailureTestData))]
    public void ItemsResults_CreateFailure_SetsExpectedProperties(string _, object defaultFailureResult)
    {
        // Default failure
        Assert.False(((dynamic)defaultFailureResult).Success);
        Assert.Null(((dynamic)defaultFailureResult).Value);
        Assert.Equal(ApiErrorCode.Failure, ((dynamic)defaultFailureResult).ErrorCode);
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

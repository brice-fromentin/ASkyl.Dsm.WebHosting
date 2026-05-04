using System.Text.Json;
using Askyl.Dsm.WebHosting.Data.Domain.FileSystem;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Tests.Data.Results;

public class ResultSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #region ApiResult

    [Fact]
    public void ApiResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var result = ApiResult.CreateSuccess("Done");
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<ApiResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.Equal("Done", deserialized.Message);
        Assert.Equal(ApiErrorCode.None, deserialized.ErrorCode);
    }

    #endregion

    #region ApiResultBool

    [Fact]
    public void ApiResultBool_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var result = ApiResultBool.CreateSuccess(true, "OK");
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<ApiResultBool>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.True(deserialized.Value);
    }

    #endregion

    #region AuthenticationResult

    [Fact]
    public void AuthenticationResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var result = AuthenticationResult.CreateAuthenticated("Welcome");
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<AuthenticationResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.True(deserialized.IsAuthenticated);
    }

    #endregion

    #region InstallationResult

    [Fact]
    public void InstallationResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var result = InstallationResult.CreateSuccess("8.0.1");
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<InstallationResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.Equal("8.0.1", deserialized.Version);
    }

    #endregion

    #region SharedFoldersResult

    [Fact]
    public void SharedFoldersResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var entries = new List<FsEntry> { new("/path/folder", "folder", true, "/real/folder", null, DateTime.UtcNow) };
        var result = SharedFoldersResult.CreateSuccess(entries, "OK");
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<SharedFoldersResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Value);
        Assert.Single(deserialized.Value);
    }

    #endregion

    #region DirectoryContentsResult

    [Fact]
    public void DirectoryContentsResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var entries = new List<FsEntry> { new("/path/sub", "sub", true, "/real/sub", null, DateTime.UtcNow) };
        var result = DirectoryContentsResult.CreateSuccess(entries);
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<DirectoryContentsResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Value);
        Assert.Single(deserialized.Value);
    }

    #endregion

    #region DirectoryFilesResult

    [Fact]
    public void DirectoryFilesResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var entries = new List<FsEntry> { new("/path/file.txt", "file.txt", false, "/real/file.txt", 1024, DateTime.UtcNow) };
        var result = DirectoryFilesResult.CreateSuccess(entries);
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<DirectoryFilesResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Value);
        Assert.Single(deserialized.Value);
    }

    #endregion

    #region ChannelsResult

    [Fact]
    public void ChannelsResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var channel = new AspNetChannel(CreateReleaseInfo("8.0.1", "8.0", isLts: true));
        var channels = new List<AspNetChannel> { channel };
        var result = ChannelsResult.CreateSuccess(channels, "Fetched");
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<ChannelsResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Value);
        Assert.Single(deserialized.Value);
    }

    #endregion

    #region ReleasesResult

    [Fact]
    public void ReleasesResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var release = AspNetRelease.Create(CreateReleaseInfo("8.0.1", "8.0"));
        var releases = new List<AspNetRelease> { release };
        var result = ReleasesResult.CreateSuccess(releases);
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<ReleasesResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Value);
        Assert.Single(deserialized.Value);
    }

    #endregion

    #region InstalledVersionsResult

    [Fact]
    public void InstalledVersionsResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var versions = new List<FrameworkInfo> { new("Microsoft.NETCore.App", "8.0.1") };
        var result = InstalledVersionsResult.CreateSuccess(versions);
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<InstalledVersionsResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Value);
        Assert.Single(deserialized.Value);
    }

    #endregion

    #region WebSiteInstanceResult

    [Fact]
    public void WebSiteInstanceResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var instance = new WebSiteInstance(new WebSiteConfiguration { Name = "Test" });
        var result = WebSiteInstanceResult.CreateSuccess(instance, "Added");
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<WebSiteInstanceResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Value);
    }

    #endregion

    #region WebSiteInstancesResult

    [Fact]
    public void WebSiteInstancesResult_RoundTrip_DeserializesCorrectly()
    {
        // Arrange
        var instances = new List<WebSiteInstance> { new(new WebSiteConfiguration { Name = "Test" }) };
        var result = WebSiteInstancesResult.CreateSuccess(instances);
        var json = JsonSerializer.Serialize(result, JsonOptions);

        // Act
        var deserialized = JsonSerializer.Deserialize<WebSiteInstancesResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.NotNull(deserialized.Value);
        Assert.Single(deserialized.Value);
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

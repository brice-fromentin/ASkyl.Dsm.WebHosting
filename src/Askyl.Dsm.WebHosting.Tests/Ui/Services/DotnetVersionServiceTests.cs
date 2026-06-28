using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DotnetVersionServiceTests
{
    readonly Mock<ILogger<ILogDotnetVersionService>> _logger;
    readonly Mock<ILocalizer> _localizer;
    readonly Mock<IVersionsDetectorService> _versionsDetector;
    readonly Mock<IDownloaderService> _downloader;

    public DotnetVersionServiceTests()
    {
        _logger = new Mock<ILogger<ILogDotnetVersionService>>();
        _localizer = new Mock<ILocalizer>();
        _versionsDetector = new Mock<IVersionsDetectorService>();
        _downloader = new Mock<IDownloaderService>();
        _localizer.Setup(l => l[LK.Error.OperationFailed]).Returns("Operation failed");
    }

    DotnetVersionService CreateService()
    {
        return new DotnetVersionService(_logger.Object, _localizer.Object, _versionsDetector.Object, _downloader.Object);
    }

    #region GetInstalledVersionsAsync

    [Fact]
    public async Task GetInstalledVersionsAsync_ReturnsVersions()
    {
        // Arrange
        var expectedVersions = new List<FrameworkInfo>
        {
            new() { Type = "aspnetcore", Version = "8.0.5" },
            new() { Type = "aspnetcore", Version = "8.0.3" }
        };
        _versionsDetector.Setup(d => d.GetInstalledVersionsAsync())
            .ReturnsAsync(expectedVersions);

        var service = CreateService();

        // Act
        var result = await service.GetInstalledVersionsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("8.0.5", result.Value[0].Version);
        Assert.Equal("8.0.3", result.Value[1].Version);
    }

    [Fact]
    public async Task GetInstalledVersionsAsync_ReturnsEmpty_WhenNoneInstalled()
    {
        // Arrange
        _versionsDetector.Setup(d => d.GetInstalledVersionsAsync())
            .ReturnsAsync([]);

        var service = CreateService();

        // Act
        var result = await service.GetInstalledVersionsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task GetInstalledVersionsAsync_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _versionsDetector.Setup(d => d.GetInstalledVersionsAsync())
            .ThrowsAsync(new IOException("Process failed"));

        var service = CreateService();

        // Act
        var result = await service.GetInstalledVersionsAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Operation failed", result.Message);
    }

    #endregion

    #region IsChannelInstalledAsync

    [Fact]
    public async Task IsChannelInstalledAsync_ReturnsTrue_WhenInstalled()
    {
        // Arrange
        _versionsDetector.Setup(d => d.IsChannelInstalled("8.0", DotNetFrameworkTypes.AspNetCore))
            .Returns(true);

        var service = CreateService();

        // Act
        var result = await service.IsChannelInstalledAsync("8.0");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task IsChannelInstalledAsync_ReturnsFalse_WhenNotInstalled()
    {
        // Arrange
        _versionsDetector.Setup(d => d.IsChannelInstalled("9.0", DotNetFrameworkTypes.AspNetCore))
            .Returns(false);

        var service = CreateService();

        // Act
        var result = await service.IsChannelInstalledAsync("9.0");

        // Assert
        Assert.True(result.Success);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task IsChannelInstalledAsync_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _versionsDetector.Setup(d => d.IsChannelInstalled("8.0", DotNetFrameworkTypes.AspNetCore))
            .Throws(new IOException("Process failed"));

        var service = CreateService();

        // Act
        var result = await service.IsChannelInstalledAsync("8.0");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Operation failed", result.Message);
    }

    #endregion

    #region IsVersionInstalledAsync

    [Fact]
    public async Task IsVersionInstalledAsync_ReturnsTrue_WhenInstalled()
    {
        // Arrange
        _versionsDetector.Setup(d => d.IsVersionInstalled("8.0.5", DotNetFrameworkTypes.AspNetCore))
            .Returns(true);

        var service = CreateService();

        // Act
        var result = await service.IsVersionInstalledAsync("8.0.5");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task IsVersionInstalledAsync_ReturnsFalse_WhenNotInstalled()
    {
        // Arrange
        _versionsDetector.Setup(d => d.IsVersionInstalled("8.0.9", DotNetFrameworkTypes.AspNetCore))
            .Returns(false);

        var service = CreateService();

        // Act
        var result = await service.IsVersionInstalledAsync("8.0.9");

        // Assert
        Assert.True(result.Success);
        Assert.False(result.Value);
    }

    #endregion

    #region RefreshCacheAsync

    [Fact]
    public async Task RefreshCacheAsync_DelegatesToDetector()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.RefreshCacheAsync();

        // Assert
        _versionsDetector.Verify(d => d.RefreshCacheAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region IsValidVersionFormat

    [Fact]
    public void IsValidVersionFormat_ValidTwoPart_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsValidVersionFormat("8.0");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidVersionFormat_ValidThreePart_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsValidVersionFormat("8.0.5");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidVersionFormat_EmptyString_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsValidVersionFormat(String.Empty);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidVersionFormat_Null_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsValidVersionFormat(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidVersionFormat_Whitespace_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsValidVersionFormat("   ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidVersionFormat_InvalidFormat_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsValidVersionFormat("invalid");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidVersionFormat_Letters_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = service.IsValidVersionFormat("8.0.rc1");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetChannelsAsync

    [Fact]
    public async Task GetChannelsAsync_ReturnsChannels()
    {
        // Arrange
        _versionsDetector.Setup(d => d.GetInstalledVersionsAsync())
            .ReturnsAsync([]);
        _downloader.Setup(d => d.GetAspNetCoreChannelsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new("8.0.5", "8.0", null, false, true, AspNetCoreReleaseType.LTS),
                new("9.0.0", "9.0", null, false, false, AspNetCoreReleaseType.STS)
            ]);

        var service = CreateService();

        // Act
        var result = await service.GetChannelsAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task GetChannelsAsync_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _versionsDetector.Setup(d => d.GetInstalledVersionsAsync())
            .ThrowsAsync(new IOException("Process failed"));

        var service = CreateService();

        // Act
        var result = await service.GetChannelsAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Operation failed", result.Message);
    }

    #endregion

    #region GetReleasesWithStatusAsync

    [Fact]
    public async Task GetReleasesWithStatusAsync_ReturnsReleasesWithStatus()
    {
        // Arrange
        var releases = new List<AspNetCoreReleaseInfo>
        {
            new("8.0.5", "8.0", null, false, true, AspNetCoreReleaseType.LTS),
            new("8.0.3", "8.0", null, false, true, AspNetCoreReleaseType.LTS)
        };
        _downloader.Setup(d => d.GetAspNetCoreReleasesAsync("8.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(releases);
        _versionsDetector.Setup(d => d.IsVersionInstalled("8.0.5", DotNetFrameworkTypes.AspNetCore))
            .Returns(true);
        _versionsDetector.Setup(d => d.IsVersionInstalled("8.0.3", DotNetFrameworkTypes.AspNetCore))
            .Returns(false);

        var service = CreateService();

        // Act
        var result = await service.GetReleasesWithStatusAsync("8.0");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.True(result.Value[0].IsInstalled);
        Assert.False(result.Value[1].IsInstalled);
    }

    [Fact]
    public async Task GetReleasesWithStatusAsync_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _downloader.Setup(d => d.GetAspNetCoreReleasesAsync("8.0", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Network error"));

        var service = CreateService();

        // Act
        var result = await service.GetReleasesWithStatusAsync("8.0");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Operation failed", result.Message);
    }

    #endregion
}

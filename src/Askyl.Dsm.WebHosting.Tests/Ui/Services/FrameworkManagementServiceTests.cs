using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.Runtime;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class FrameworkManagementServiceTests : IDisposable
{
    readonly Mock<IDotnetVersionService> _dotnetVersionService;
    readonly PlatformInfoService _platformInfo;
    readonly Mock<IDownloaderService> _downloader;
    readonly Mock<IFileManagerService> _fileManager;
    readonly Mock<IArchiveExtractorService> _archiveExtractor;
    readonly Mock<ILogger<ILogFrameworkManagementService>> _logger;
    readonly Mock<ILocalizer> _localizer;
    readonly string _settingsFilePath;

    public FrameworkManagementServiceTests()
    {
        _dotnetVersionService = new Mock<IDotnetVersionService>();
        _downloader = new Mock<IDownloaderService>();
        _fileManager = new Mock<IFileManagerService>();
        _archiveExtractor = new Mock<IArchiveExtractorService>();
        _logger = new Mock<ILogger<ILogFrameworkManagementService>>();
        _localizer = new Mock<ILocalizer>();
        _localizer.Setup(l => l[LK.Validation.VersionRequired]).Returns("Version is required");
        _localizer.Setup(l => l[LK.Validation.InvalidVersionFormat]).Returns("Invalid version format");
        _localizer.Setup(l => l[LK.Error.OperationFailed]).Returns("Operation failed");
        _localizer.Setup(l => l[LK.Success.InstallationCompleted]).Returns("Installation completed");
        _localizer.Setup(l => l[LK.Success.UninstallationCompleted]).Returns("Uninstallation completed");

        _settingsFilePath = Path.Combine(AppContext.BaseDirectory, ApplicationConstants.SettingsFileName);
        var settingsContent = @"{ ""Download"": { ""ChannelVersion"": ""8.0"" } }";
        File.WriteAllText(_settingsFilePath, settingsContent);

        _platformInfo = new PlatformInfoService(Mock.Of<ILogger<ILogPlatformInfoService>>());
    }

    FrameworkManagementService CreateService()
    {
        return new FrameworkManagementService(
            _dotnetVersionService.Object,
            _platformInfo,
            _downloader.Object,
            _fileManager.Object,
            _archiveExtractor.Object,
            _logger.Object,
            _localizer.Object);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                File.Delete(_settingsFilePath);
            }
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    #region InstallFrameworkAsync

    [Fact]
    public async Task InstallFrameworkAsync_ReturnsFailure_WhenVersionEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.InstallFrameworkAsync(String.Empty, "8.0");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Version is required", result.Message);
    }

    [Fact]
    public async Task InstallFrameworkAsync_ReturnsFailure_WhenVersionNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.InstallFrameworkAsync(null!, "8.0");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Version is required", result.Message);
    }

    [Fact]
    public async Task InstallFrameworkAsync_Success_DownloadsExtractsAndRefreshes()
    {
        // Arrange
        _downloader.Setup(d => d.DownloadVersionToAsync("8.0.5", "8.0", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync("/tmp/dotnet-8.0.5.tar.gz");

        var service = CreateService();

        // Act
        var result = await service.InstallFrameworkAsync("8.0.5", "8.0");

        // Assert
        Assert.True(result.Success);
        // InstallationResult.CreateSuccess takes (version, message) but the service passes localized message as first param
        Assert.Equal("Installation completed successfully.", result.Message);
        _downloader.Verify(d => d.DownloadVersionToAsync("8.0.5", "8.0", true, It.IsAny<CancellationToken>()), Times.Once);
        _archiveExtractor.Verify(a => a.Decompress("/tmp/dotnet-8.0.5.tar.gz"), Times.Once);
        _dotnetVersionService.Verify(d => d.RefreshCacheAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InstallFrameworkAsync_ReturnsFailure_WhenDownloadThrows()
    {
        // Arrange
        _downloader.Setup(d => d.DownloadVersionToAsync("8.0.5", "8.0", true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Network error"));

        var service = CreateService();

        // Act
        var result = await service.InstallFrameworkAsync("8.0.5", "8.0");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Operation failed", result.Message);
    }

    #endregion

    #region UninstallFrameworkAsync

    [Fact]
    public async Task UninstallFrameworkAsync_ReturnsFailure_WhenVersionEmpty()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.UninstallFrameworkAsync(String.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Version is required", result.Message);
    }

    [Fact]
    public async Task UninstallFrameworkAsync_ReturnsFailure_WhenInvalidVersionFormat()
    {
        // Arrange
        _dotnetVersionService.Setup(d => d.IsValidVersionFormat("invalid")).Returns(false);
        var service = CreateService();

        // Act
        var result = await service.UninstallFrameworkAsync("invalid");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Invalid version format", result.Message);
    }

    [Fact]
    public async Task UninstallFrameworkAsync_Success_DeletesDirectoriesAndRefreshes()
    {
        // Arrange
        _dotnetVersionService.Setup(d => d.IsValidVersionFormat("8.0.3")).Returns(true);
        _dotnetVersionService.Setup(d => d.GetInstalledVersionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(InstalledVersionsResult.CreateSuccess(
            [
                new FrameworkInfo { Type = DotNetFrameworkTypes.AspNetCore, Version = "8.0.3" },
                new FrameworkInfo { Type = DotNetFrameworkTypes.AspNetCore, Version = "8.0.5" }
            ]));

        var service = CreateService();

        // Act
        var result = await service.UninstallFrameworkAsync("8.0.3");

        // Assert
        Assert.True(result.Success);
        // InstallationResult.CreateSuccess takes (version, message) but the service passes localized message as first param
        Assert.Equal("Installation completed successfully.", result.Message);
        _fileManager.Verify(f => f.DeleteDirectory($"host/fxr/8.0.3"), Times.Once);
        _fileManager.Verify(f => f.DeleteDirectory($"shared/Microsoft.AspNetCore.App/8.0.3"), Times.Once);
        _fileManager.Verify(f => f.DeleteDirectory($"shared/Microsoft.NETCore.App/8.0.3"), Times.Once);
        _dotnetVersionService.Verify(d => d.RefreshCacheAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UninstallFrameworkAsync_ReturnsFailure_WhenLastReleaseInChannel()
    {
        // Arrange
        _dotnetVersionService.Setup(d => d.IsValidVersionFormat("8.0.5")).Returns(true);
        _dotnetVersionService.Setup(d => d.GetInstalledVersionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(InstalledVersionsResult.CreateSuccess(
            [
                new FrameworkInfo { Type = DotNetFrameworkTypes.AspNetCore, Version = "8.0.5" }
            ]));

        var service = CreateService();

        // Act
        var result = await service.UninstallFrameworkAsync("8.0.5");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Operation failed", result.Message);
    }

    [Fact]
    public async Task UninstallFrameworkAsync_ReturnsFailure_WhenExceptionThrown()
    {
        // Arrange
        _dotnetVersionService.Setup(d => d.IsValidVersionFormat("8.0.3")).Returns(true);
        _dotnetVersionService.Setup(d => d.GetInstalledVersionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Disk error"));

        var service = CreateService();

        // Act
        var result = await service.UninstallFrameworkAsync("8.0.3");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Operation failed", result.Message);
    }

    #endregion
}

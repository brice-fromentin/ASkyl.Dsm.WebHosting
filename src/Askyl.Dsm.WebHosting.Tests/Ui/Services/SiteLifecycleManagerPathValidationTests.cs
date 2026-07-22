using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

/// <summary>
/// Tests for ApplicationRealPath directory boundary validation in SiteLifecycleManager.
/// Ensures application executables are within /volume*/shared/ or /volume*/web/ before process spawn.
/// </summary>
public class SiteLifecycleManagerPathValidationTests
{
    private readonly Mock<ILogger<ILogSiteLifecycleManager>> _logger;
    private readonly Mock<ILocalizer> _localizer;
    private readonly Mock<IProcessRunner> _processRunner;
    private readonly Mock<IAssemblyRuntimeDetector> _detector;

    public SiteLifecycleManagerPathValidationTests()
    {
        _logger = new Mock<ILogger<ILogSiteLifecycleManager>>();
        _localizer = new Mock<ILocalizer>();
        _processRunner = new Mock<IProcessRunner>();
        _detector = new Mock<IAssemblyRuntimeDetector>();
    }

    private SiteLifecycleManager CreateManager(WebSiteConfiguration configuration)
    {
        return new SiteLifecycleManager(_logger.Object, _localizer.Object, _processRunner.Object, _detector.Object, configuration);
    }

    private WebSiteConfiguration CreateConfig(string applicationRealPath, string name = "TestSite")
    {
        return new WebSiteConfiguration
        {
            Id = Guid.NewGuid(),
            Name = name,
            ApplicationPath = Path.GetDirectoryName(applicationRealPath) ?? "",
            ApplicationRealPath = applicationRealPath,
            InternalPort = 5001,
            HostName = "test.local",
            Environment = "Production"
        };
    }

    #region Path outside /volume* - must reject

    [Fact]
    public async Task StartAsync_PathOutsideVolume_DeniesStart()
    {
        // Arrange
        var config = CreateConfig("/tmp/myapp/app.dll");
        var manager = CreateManager(config);

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
        _processRunner.Verify(r => r.Start(It.IsAny<System.Diagnostics.ProcessStartInfo>()), Times.Never);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_PathInHomeDirectory_DeniesStart()
    {
        // Arrange
        var config = CreateConfig("/home/admin/myapp/app.dll");
        var manager = CreateManager(config);

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
        _processRunner.Verify(r => r.Start(It.IsAny<System.Diagnostics.ProcessStartInfo>()), Times.Never);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_PathInEtc_DeniesStart()
    {
        // Arrange
        var config = CreateConfig("/etc/myapp/app.dll");
        var manager = CreateManager(config);

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
        _processRunner.Verify(r => r.Start(It.IsAny<System.Diagnostics.ProcessStartInfo>()), Times.Never);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_RelativePath_DeniesStart()
    {
        // Arrange
        var config = CreateConfig("./myapp/app.dll");
        var manager = CreateManager(config);

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
        _processRunner.Verify(r => r.Start(It.IsAny<System.Diagnostics.ProcessStartInfo>()), Times.Never);

        manager.Dispose();
    }

    #endregion

    #region Path in /volume* but not /shared/ or /web/ - must reject

    [Fact]
    public async Task StartAsync_VolumeButNotSharedOrWeb_DeniesStart()
    {
        // Arrange
        var config = CreateConfig("/volume1/homes/user/myapp/app.dll");
        var manager = CreateManager(config);

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
        _processRunner.Verify(r => r.Start(It.IsAny<System.Diagnostics.ProcessStartInfo>()), Times.Never);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_VolumeRootDirectly_DeniesStart()
    {
        // Arrange
        var config = CreateConfig("/volume1/app.dll");
        var manager = CreateManager(config);

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
        _processRunner.Verify(r => r.Start(It.IsAny<System.Diagnostics.ProcessStartInfo>()), Times.Never);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_VolumeWithNoSharedOrWebSubdir_DeniesStart()
    {
        // Arrange
        var config = CreateConfig("/volume2/@ea_dir/myapp/app.dll");
        var manager = CreateManager(config);

        // Act
        var result = await manager.StartAsync();

        // Assert
        Assert.False(result.Success);
        _processRunner.Verify(r => r.Start(It.IsAny<System.Diagnostics.ProcessStartInfo>()), Times.Never);

        manager.Dispose();
    }

    #endregion

    #region Path boundary logging verification

    [Fact]
    public async Task StartAsync_InvalidPath_LogsBlockedWarning()
    {
        // Arrange
        var config = CreateConfig("/tmp/myapp/app.dll", "BlockedSite");
        var manager = CreateManager(config);

        // Act
        await manager.StartAsync();

        // Assert
        _logger.Verify(l => l.ApplicationPathBlocked("/tmp/myapp/app.dll", "BlockedSite"), Times.Once);

        manager.Dispose();
    }

    [Fact]
    public async Task StartAsync_VolumeButWrongSubdir_LogsBlockedWarning()
    {
        // Arrange
        var config = CreateConfig("/volume1/homes/user/app.dll", "WrongSubdirSite");
        var manager = CreateManager(config);

        // Act
        await manager.StartAsync();

        // Assert
        _logger.Verify(l => l.ApplicationPathBlocked("/volume1/homes/user/app.dll", "WrongSubdirSite"), Times.Once);

        manager.Dispose();
    }

    #endregion

    #region Path does not start with /volume - logging before file check

    [Fact]
    public async Task StartAsync_NonVolumePath_LogsBeforeFileCheck()
    {
        // Arrange - path that doesn't start with /volume, so validation fails before File.Exists
        var config = CreateConfig("/tmp/nonexistent/app.dll", "EarlyValidation");
        var manager = CreateManager(config);

        // Act
        await manager.StartAsync();

        // Assert - ApplicationPathBlocked is logged (validation runs before File.Exists check)
        _logger.Verify(l => l.ApplicationPathBlocked(It.IsAny<string>(), "EarlyValidation"), Times.Once);
        // ApplicationBinaryNotFound should NOT be called since validation throws first
        _logger.Verify(l => l.ApplicationBinaryNotFound(It.IsAny<string>()), Times.Never);

        manager.Dispose();
    }

    #endregion
}

using System.Diagnostics;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Validators;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

[Trait("Category", "FileSystem")]
public class WebSiteHostingServiceTests : IDisposable
{
    readonly Mock<ILogger<ILogWebSiteHostingService>> _logger;
    readonly Mock<ILogger<ILogWebSitesConfigurationService>> _configLogger;
    readonly Mock<ILoggerFactory> _loggerFactory;
    readonly Mock<IProcessRunner> _processRunner;
    readonly Mock<IServiceScopeFactory> _scopeFactory;
    readonly Mock<IServiceScope> _serviceScope;
    readonly Mock<IServiceProvider> _serviceProvider;
    readonly Mock<IAssemblyRuntimeDetector> _assemblyRuntimeDetector;
    readonly Mock<IVersionsDetectorService> _versionsDetector;
    readonly Mock<ILocalizer> _localizer;
    readonly Mock<IFileSystemService> _fileSystemService;
    readonly Mock<IReverseProxyManagerService> _reverseProxyManager;
    readonly string _tempDir;

    public WebSiteHostingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"asm_host_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _logger = new Mock<ILogger<ILogWebSiteHostingService>>();
        _configLogger = new Mock<ILogger<ILogWebSitesConfigurationService>>();
        _loggerFactory = new Mock<ILoggerFactory>();
        _processRunner = new Mock<IProcessRunner>();
        _scopeFactory = new Mock<IServiceScopeFactory>();
        _serviceScope = new Mock<IServiceScope>();
        _serviceProvider = new Mock<IServiceProvider>();
        _assemblyRuntimeDetector = new Mock<IAssemblyRuntimeDetector>();
        _versionsDetector = new Mock<IVersionsDetectorService>();
        _localizer = new Mock<ILocalizer>();
        _fileSystemService = new Mock<IFileSystemService>();
        _reverseProxyManager = new Mock<IReverseProxyManagerService>();

        // CreateLogger<T>() wraps factory.CreateLogger(name); an unmocked factory produces a null-backed
        // logger whose first log call throws, so hand back a real no-op logger.
        _loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

        _localizer.Setup(l => l[LK.Error.OperationFailed]).Returns("Operation failed");
        _localizer.Setup(l => l[LK.Error.InstanceNotFound]).Returns("Instance not found");
        _localizer.Setup(l => l[LK.Error.SiteNotFound, It.IsAny<object>()]).Returns("Site not found");
        _localizer.Setup(l => l[LK.Validation.EnvVarKeyTooLong, It.IsAny<object>(), It.IsAny<object>()]).Returns("Environment variable key too long");
        _localizer.Setup(l => l[LK.Validation.EnvVarValueTooLong, It.IsAny<object>(), It.IsAny<object>()]).Returns("Environment variable value too long");
        _localizer.Setup(l => l[LK.Error.RuntimeDetectionFailed]).Returns("Runtime detection failed");
        _localizer.Setup(l => l[LK.Error.RuntimeNotInstalled, It.IsAny<object>()]).Returns("Runtime not installed");

        _scopeFactory.Setup(f => f.CreateScope()).Returns(_serviceScope.Object);
        _serviceScope.As<IDisposable>().Setup(d => d.Dispose());
        _serviceProvider.Setup(p => p.GetService(It.IsAny<Type>()))
            .Returns((Type type) =>
            {
                if (type == typeof(IFileSystemService))
                {
                    return _fileSystemService.Object;
                }

                if (type == typeof(IReverseProxyManagerService))
                {
                    return _reverseProxyManager.Object;
                }

                return null;
            });
        _serviceScope.Setup(s => s.ServiceProvider).Returns(_serviceProvider.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    WebSiteHostingService CreateService()
    {
        var configService = new WebSitesConfigurationService(_configLogger.Object, _tempDir);
        return new WebSiteHostingService(
            _logger.Object,
            _loggerFactory.Object,
            _processRunner.Object,
            configService,
            _scopeFactory.Object,
            _assemblyRuntimeDetector.Object,
            _versionsDetector.Object,
            new WebSiteConfigurationValidator(),
            _localizer.Object);
    }

    /// <summary>
    /// Configuration pointing at a real file so the lifecycle manager's existence check passes.
    /// </summary>
    WebSiteConfiguration CreateRunnableConfiguration()
    {
        return new WebSiteConfiguration
        {
            Name = "TestSite",
            ApplicationPath = _tempDir,
            ApplicationRealPath = Path.Combine(_tempDir, "MyApp.dll"),
            InternalPort = 5001,
            HostName = "test.local"
        };
    }

    /// <summary>
    /// Makes the runner hand back a live process handle and stubs the update side effects.
    /// The handle reports itself exited once signalled, mirroring a real process — a handle stuck
    /// at HasExited=false would leave the instance permanently "running" and hide restart bugs.
    /// </summary>
    Mock<IProcessHandle> SetupRunningProcess()
    {
        File.WriteAllText(Path.Combine(_tempDir, "MyApp.dll"), String.Empty);

        var exited = false;
        var handle = new Mock<IProcessHandle>();

        handle.SetupGet(h => h.Id).Returns(4242);
        handle.SetupGet(h => h.HasExited).Returns(() => exited);
        handle.Setup(h => h.SendGracefulShutdownSignal()).Callback(() => exited = true);
        handle.Setup(h => h.Kill()).Callback(() => exited = true);

        _processRunner.Setup(r => r.Start(It.IsAny<ProcessStartInfo>())).Returns(handle.Object);

        _fileSystemService.Setup(f => f.SetHttpGroupPermissionsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.CreateSuccess());
        _reverseProxyManager.Setup(r => r.CreateAsync(It.IsAny<WebSiteConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _reverseProxyManager.Setup(r => r.UpdateAsync(It.IsAny<WebSiteConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _assemblyRuntimeDetector.Setup(d => d.Detect(It.IsAny<string>())).Returns((AssemblyRuntimeInfo?)null);

        return handle;
    }

    /// <summary>
    /// Adds a site and returns its id. No explicit start is needed — AddWebsiteAsync starts the site
    /// itself when IsEnabled and AutoStart are set, and both default to true.
    /// </summary>
    async Task<Guid> AddRunningSiteAsync(WebSiteHostingService service)
    {
        var added = await service.AddWebsiteAsync(CreateRunnableConfiguration());

        Assert.True(added.Success);
        Assert.NotNull(added.Value);
        Assert.True(added.Value.IsRunning);

        return added.Value.Id;
    }

    #region GetAllWebsitesAsync

    [Fact]
    public async Task GetAllWebsitesAsync_ReturnsEmpty_WhenNone()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetAllWebsitesAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    #endregion

    #region StartWebsiteAsync

    [Fact]
    public async Task StartWebsiteAsync_ReturnsFailure_WhenSiteNotFound()
    {
        // Arrange
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.StartWebsiteAsync(nonExistentId);

        // Assert
        Assert.False(result.Success);
        Assert.NotEqual(ApiErrorCode.None, result.ErrorCode);
    }

    #endregion

    #region StopWebsiteAsync

    [Fact]
    public async Task StopWebsiteAsync_ReturnsFailure_WhenSiteNotFound()
    {
        // Arrange
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.StopWebsiteAsync(nonExistentId);

        // Assert
        Assert.False(result.Success);
        Assert.NotEqual(ApiErrorCode.None, result.ErrorCode);
    }

    #endregion

    #region RemoveWebsiteAsync

    [Fact]
    public async Task RemoveWebsiteAsync_ReturnsFailure_WhenSiteNotFound()
    {
        // Arrange
        var service = CreateService();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await service.RemoveWebsiteAsync(nonExistentId);

        // Assert
        Assert.False(result.Success);
        Assert.NotEqual(ApiErrorCode.None, result.ErrorCode);
    }

    #endregion

    #region UpdateWebsiteAsync

    [Fact]
    public async Task UpdateWebsiteAsync_ReturnsFailure_WhenSiteNotFound()
    {
        // Arrange
        var service = CreateService();
        var config = new WebSiteConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "TestSite"
        };

        // Act
        var result = await service.UpdateWebsiteAsync(config);

        // Assert
        Assert.False(result.Success);
        Assert.NotEqual(ApiErrorCode.None, result.ErrorCode);
    }

    [Fact]
    public async Task UpdateWebsiteAsync_LeavesProcessRunning_WhenChangeRequiresNoRestart()
    {
        // Arrange — a rename is deliberately excluded from ConfigurationRequiresRestart, so the
        // running process must survive it untouched: no kill, no replacement process.
        var handle = SetupRunningProcess();
        var service = CreateService();
        var id = await AddRunningSiteAsync(service);

        // Act
        var renamed = CreateRunnableConfiguration();
        renamed.Id = id;
        renamed.Name = "RenamedSite";

        var result = await service.UpdateWebsiteAsync(renamed);

        // Assert
        Assert.True(result.Success);
        _processRunner.Verify(r => r.Start(It.IsAny<ProcessStartInfo>()), Times.Once);
        handle.Verify(h => h.Kill(), Times.Never);
        handle.Verify(h => h.SendGracefulShutdownSignal(), Times.Never);
    }

    [Fact]
    public async Task UpdateWebsiteAsync_RestartsProcess_WhenPortChanges()
    {
        // Arrange — the opposite guard: a port change IS in ConfigurationRequiresRestart, so the
        // site must be stopped and started again rather than left on the stale port.
        var handle = SetupRunningProcess();
        var service = CreateService();
        var id = await AddRunningSiteAsync(service);

        // Act
        var moved = CreateRunnableConfiguration();
        moved.Id = id;
        moved.InternalPort += 1;

        var result = await service.UpdateWebsiteAsync(moved);

        // Assert
        Assert.True(result.Success);
        _processRunner.Verify(r => r.Start(It.IsAny<ProcessStartInfo>()), Times.Exactly(2));
        handle.Verify(h => h.SendGracefulShutdownSignal(), Times.Once);
    }

    #endregion

    #region Reverse Proxy Compensation

    [Fact]
    public async Task AddWebsiteAsync_WhenPersistenceFails_DeletesTheReverseProxyRuleItCreated()
    {
        // Arrange — a duplicate name is rejected by AddSiteAsync, after the rule has been created in
        // DSM. Nothing in this application lists DSM rules, so one left behind is invisible from here.
        SetupRunningProcess();
        _reverseProxyManager.Setup(r => r.DeleteAsync(It.IsAny<WebSiteConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await AddRunningSiteAsync(service);

        var duplicate = CreateRunnableConfiguration();

        // Act
        var result = await service.AddWebsiteAsync(duplicate);

        // Assert
        Assert.False(result.Success);
        _reverseProxyManager.Verify(r => r.CreateAsync(duplicate, It.IsAny<CancellationToken>()), Times.Once);
        _reverseProxyManager.Verify(r => r.DeleteAsync(duplicate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddWebsiteAsync_WhenCancelledAfterTheRuleIsCreated_StillRemovesIt()
    {
        // Arrange — cancel between step 2 and step 3, the exact window in which the rule exists and the
        // configuration does not. Persistence only observes that if the token reaches it, and the
        // compensating delete only runs if it does not itself honour the cancellation.
        SetupRunningProcess();

        using var cancellation = new CancellationTokenSource();

        _reverseProxyManager.Setup(r => r.CreateAsync(It.IsAny<WebSiteConfiguration>(), It.IsAny<CancellationToken>()))
            .Callback(cancellation.Cancel)
            .Returns(Task.CompletedTask);
        _reverseProxyManager.Setup(r => r.DeleteAsync(It.IsAny<WebSiteConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var configuration = CreateRunnableConfiguration();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.AddWebsiteAsync(configuration, cancellation.Token));

        _reverseProxyManager.Verify(r => r.DeleteAsync(configuration, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateWebsiteAsync_WhenPersistenceFails_RestoresThePreviousReverseProxyRule()
    {
        // Arrange — two sites, then rename the second onto the first's name. UpdateSiteAsync rejects
        // that, but DSM has already been pointed at the new configuration.
        SetupRunningProcess();

        var service = CreateService();

        await AddRunningSiteAsync(service);

        var second = CreateRunnableConfiguration();
        second.Name = "SecondSite";
        second.InternalPort = 5002;

        var added = await service.AddWebsiteAsync(second);

        Assert.True(added.Success);

        var renamed = CreateRunnableConfiguration();
        renamed.Id = added.Value!.Id;
        renamed.InternalPort = 5002;

        _reverseProxyManager.Invocations.Clear();

        // Act
        var result = await service.UpdateWebsiteAsync(renamed);

        // Assert
        Assert.False(result.Success);
        _reverseProxyManager.Verify(r => r.UpdateAsync(renamed, It.IsAny<CancellationToken>()), Times.Once);

        // DSM must end up describing what is on disk, which is still the site under its own name.
        _reverseProxyManager.Verify(
            r => r.UpdateAsync(It.Is<WebSiteConfiguration>(c => c.Name == "SecondSite"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion


    #region AddWebsiteAsync - Validation

    [Fact]
    public async Task AddWebsiteAsync_RejectsInvalidConfiguration_WithoutSideEffects()
    {
        // Arrange — validation moved out of the model-binding filter into the service, so the
        // service must refuse invalid input itself, before touching permissions or the proxy.
        var service = CreateService();
        var config = CreateRunnableConfiguration();
        config.Name = String.Empty;
        config.HostName = "not a valid host name";

        // Act
        var result = await service.AddWebsiteAsync(config);

        // Assert — compared against the validator's own output rather than a literal, so the
        // assertion holds under any culture and proves the message came from the validator.
        var expected = new WebSiteConfigurationValidator().Validate(config).ToMessage();

        Assert.False(result.Success);
        Assert.Equal(expected, result.Message);
        _fileSystemService.Verify(f => f.SetHttpGroupPermissionsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _reverseProxyManager.Verify(r => r.CreateAsync(It.IsAny<WebSiteConfiguration>(), It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public async Task AddWebsiteAsync_ReturnsFailure_WhenEnvVarKeyTooLong()
    {
        // Arrange
        var service = CreateService();
        var longKey = new string('A', 300);
        var config = new WebSiteConfiguration
        {
            Name = "TestSite",
            ApplicationPath = "/volume1/web",
            ApplicationRealPath = "/volume1/web/app.dll",
            InternalPort = 5001,
            HostName = "test.local",
            AdditionalEnvironmentVariables = new Dictionary<string, string> { [longKey] = "value" }
        };

        // Act
        var result = await service.AddWebsiteAsync(config);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Environment variable key too long", result.Message);
    }

    [Fact]
    public async Task AddWebsiteAsync_ReturnsFailure_WhenEnvVarValueTooLong()
    {
        // Arrange
        var service = CreateService();
        var longValue = new string('B', 10001);
        var config = new WebSiteConfiguration
        {
            Name = "TestSite",
            ApplicationPath = "/volume1/web",
            ApplicationRealPath = "/volume1/web/app.dll",
            InternalPort = 5001,
            HostName = "test.local",
            AdditionalEnvironmentVariables = new Dictionary<string, string> { ["KEY"] = longValue }
        };

        // Act
        var result = await service.AddWebsiteAsync(config);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Environment variable value too long", result.Message);
    }

    [Fact]
    public async Task AddWebsiteAsync_ReturnsFailure_WhenEnvVarKeyIsEmpty()
    {
        // Arrange
        var service = CreateService();
        var config = new WebSiteConfiguration
        {
            Name = "TestSite",
            ApplicationPath = "/volume1/web",
            ApplicationRealPath = "/volume1/web/app.dll",
            InternalPort = 5001,
            HostName = "test.local",
            AdditionalEnvironmentVariables = new Dictionary<string, string> { [""] = "value" }
        };

        // Act
        var result = await service.AddWebsiteAsync(config);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Environment variable key too long", result.Message);
    }

    [Fact]
    public async Task AddWebsiteAsync_Succeeds_WithValidConfiguration()
    {
        // Arrange
        var service = CreateService();
        var config = new WebSiteConfiguration
        {
            Name = "TestSite",
            ApplicationPath = "/volume1/web/app",
            ApplicationRealPath = "/volume1/web/app/MyApp.dll",
            InternalPort = 5001,
            HostName = "test.local"
        };

        _fileSystemService.Setup(f => f.SetHttpGroupPermissionsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.CreateSuccess());
        _reverseProxyManager.Setup(r => r.CreateAsync(It.IsAny<WebSiteConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _assemblyRuntimeDetector.Setup(d => d.Detect(config.ApplicationRealPath))
            .Returns((AssemblyRuntimeInfo?)null);

        // Act
        var result = await service.AddWebsiteAsync(config);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal("TestSite", result.Value!.Configuration.Name);
    }

    [Fact]
    public async Task AddWebsiteAsync_Succeeds_WithRuntimeDetection()
    {
        // Arrange
        var service = CreateService();
        var config = new WebSiteConfiguration
        {
            Name = "TestSite",
            ApplicationPath = "/volume1/web/app",
            ApplicationRealPath = "/volume1/web/app/MyApp.dll",
            InternalPort = 5001,
            HostName = "test.local"
        };

        _fileSystemService.Setup(f => f.SetHttpGroupPermissionsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.CreateSuccess());
        _reverseProxyManager.Setup(r => r.CreateAsync(It.IsAny<WebSiteConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _assemblyRuntimeDetector.Setup(d => d.Detect(config.ApplicationRealPath))
            .Returns(new AssemblyRuntimeInfo("8.0", true));

        // Act
        var result = await service.AddWebsiteAsync(config);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal("8.0", result.Value!.RequiredFramework);
    }

    [Fact]
    public async Task AddWebsiteAsync_ReturnsWarning_WhenRuntimeNotInstalled()
    {
        // Arrange
        var service = CreateService();
        var config = new WebSiteConfiguration
        {
            Name = "TestSite",
            ApplicationPath = "/volume1/web/app",
            ApplicationRealPath = "/volume1/web/app/MyApp.dll",
            InternalPort = 5001,
            HostName = "test.local"
        };

        _fileSystemService.Setup(f => f.SetHttpGroupPermissionsAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.CreateSuccess());
        _reverseProxyManager.Setup(r => r.CreateAsync(It.IsAny<WebSiteConfiguration>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _assemblyRuntimeDetector.Setup(d => d.Detect(config.ApplicationRealPath))
            .Returns(new AssemblyRuntimeInfo("9.0", false));

        // Act
        var result = await service.AddWebsiteAsync(config);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.NotNull(result.Message);
    }

    #endregion
}

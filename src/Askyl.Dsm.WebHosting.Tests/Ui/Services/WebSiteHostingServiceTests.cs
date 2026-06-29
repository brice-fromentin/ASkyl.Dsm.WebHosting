using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Runtime;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

[Collection("WebSiteHostingService")]
[Trait("Category", "FileSystem")]
public class WebSiteHostingServiceTests
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
            _localizer.Object);
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
        Assert.Equal("Site not found", result.Message);
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
        Assert.Equal("Site not found", result.Message);
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
        Assert.Equal("Instance not found", result.Message);
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
        Assert.Equal("Instance not found", result.Message);
    }

    #endregion

    #region AddWebsiteAsync - Validation

    [Fact]
    public async Task AddWebsiteAsync_ReturnsFailure_WhenEnvVarKeyTooLong()
    {
        // Arrange
        var service = CreateService();
        var longKey = new string('A', 300);
        var config = new WebSiteConfiguration
        {
            Name = "TestSite",
            ApplicationRealPath = "/volume1/web/app.dll",
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
            ApplicationRealPath = "/volume1/web/app.dll",
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
            ApplicationRealPath = "/volume1/web/app.dll",
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

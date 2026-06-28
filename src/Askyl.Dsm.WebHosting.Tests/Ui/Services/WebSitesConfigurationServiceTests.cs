using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class WebSitesConfigurationServiceTests : IDisposable
{
    readonly Mock<ILogger<ILogWebSitesConfigurationService>> _logger;
    readonly string _configFilePath;
    readonly string _backupDir;

    public WebSitesConfigurationServiceTests()
    {
        _logger = new Mock<ILogger<ILogWebSitesConfigurationService>>();
        _configFilePath = Path.Combine(AppContext.BaseDirectory, WebSiteConstants.ConfigurationFileName);
        _backupDir = Path.Combine(Path.GetTempPath(), $"asm_cfg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_backupDir);
    }

    WebSitesConfigurationService CreateService()
    {
        return new WebSitesConfigurationService(_logger.Object);
    }

    void SetupEmptyConfig()
    {
        EnsureConfigFileDeleted();
        var config = new WebSitesConfiguration { Sites = [] };
        WriteConfigFile(config);
    }

    void SetupConfigWithSites(List<WebSiteConfiguration> sites)
    {
        EnsureConfigFileDeleted();
        var config = new WebSitesConfiguration { Sites = sites };
        WriteConfigFile(config);
    }

    void WriteConfigFile(WebSitesConfiguration config)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(config, System.Text.Json.JsonSerializerOptions.Default);
        File.WriteAllText(_configFilePath, json);
    }

    void EnsureConfigFileDeleted()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var backupPath = Path.Combine(_backupDir, $"{Guid.NewGuid():N}.json");
                File.Move(_configFilePath, backupPath);
            }
        }
        catch
        {
            // Best-effort
        }
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
            }
        }
        catch
        {
            // Best-effort
        }

        try
        {
            if (Directory.Exists(_backupDir))
            {
                Directory.Delete(_backupDir, recursive: true);
            }
        }
        catch
        {
            // Best-effort
        }
    }

    #region GetAllSitesAsync

    [Fact]
    public async Task GetAllSitesAsync_ReturnsEmpty_WhenNoConfigFile()
    {
        // Arrange
        EnsureConfigFileDeleted();
        var service = CreateService();

        // Act
        var result = await service.GetAllSitesAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllSitesAsync_ReturnsSites_WhenConfigExists()
    {
        // Arrange
        var sites = new List<WebSiteConfiguration>
        {
            new() { Id = Guid.NewGuid(), Name = "Site1", ApplicationPath = "/volume1/web/site1" },
            new() { Id = Guid.NewGuid(), Name = "Site2", ApplicationPath = "/volume1/web/site2" }
        };
        SetupConfigWithSites(sites);
        var service = CreateService();

        // Act
        var result = await service.GetAllSitesAsync();

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, s => s.Name == "Site1");
        Assert.Contains(resultList, s => s.Name == "Site2");
    }

    #endregion

    #region GetSiteAsync

    [Fact]
    public async Task GetSiteAsync_ReturnsSite_WhenExists()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var sites = new List<WebSiteConfiguration>
        {
            new() { Id = siteId, Name = "TestSite", ApplicationPath = "/volume1/web/test" }
        };
        SetupConfigWithSites(sites);
        var service = CreateService();

        // Act
        var result = await service.GetSiteAsync(siteId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestSite", result.Name);
        Assert.Equal(siteId, result.Id);
    }

    [Fact]
    public async Task GetSiteAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        SetupEmptyConfig();
        var service = CreateService();

        // Act
        var result = await service.GetSiteAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region AddSiteAsync

    [Fact]
    public async Task AddSiteAsync_AddsNewSite()
    {
        // Arrange
        SetupEmptyConfig();
        var service = CreateService();
        var newSite = new WebSiteConfiguration
        {
            Name = "NewSite",
            ApplicationPath = "/volume1/web/new"
        };

        // Act
        await service.AddSiteAsync(newSite);

        // Assert
        Assert.NotEqual(Guid.Empty, newSite.Id);

        var loaded = await service.GetAllSitesAsync();
        var loadedList = loaded.ToList();
        Assert.Single(loadedList);
        Assert.Equal("NewSite", loadedList[0].Name);
    }

    [Fact]
    public async Task AddSiteAsync_Throws_WhenDuplicateName()
    {
        // Arrange
        var existingSite = new WebSiteConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "ExistingSite",
            ApplicationPath = "/volume1/web/existing"
        };
        SetupConfigWithSites([existingSite]);
        var service = CreateService();

        var duplicateSite = new WebSiteConfiguration
        {
            Name = "ExistingSite",
            ApplicationPath = "/volume1/web/duplicate"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddSiteAsync(duplicateSite));
        Assert.Contains("already exists", exception.Message);
    }

    #endregion

    #region UpdateSiteAsync

    [Fact]
    public async Task UpdateSiteAsync_UpdatesExistingSite()
    {
        // Arrange
        var existingSite = new WebSiteConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "OriginalName",
            ApplicationPath = "/volume1/web/original"
        };
        SetupConfigWithSites([existingSite]);
        var service = CreateService();

        var updatedSite = new WebSiteConfiguration
        {
            Id = existingSite.Id,
            Name = "UpdatedName",
            ApplicationPath = "/volume1/web/updated"
        };

        // Act
        await service.UpdateSiteAsync(updatedSite);

        // Assert
        var loaded = await service.GetSiteAsync(existingSite.Id);
        Assert.NotNull(loaded);
        Assert.Equal("UpdatedName", loaded!.Name);
        Assert.Equal("/volume1/web/updated", loaded.ApplicationPath);
    }

    [Fact]
    public async Task UpdateSiteAsync_Throws_WhenNotFound()
    {
        // Arrange
        SetupEmptyConfig();
        var service = CreateService();
        var site = new WebSiteConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "NonExistent"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateSiteAsync(site));
        Assert.Contains("not found", exception.Message);
    }

    #endregion

    #region RemoveSiteAsync

    [Fact]
    public async Task RemoveSiteAsync_RemovesExistingSite()
    {
        // Arrange
        var siteId = Guid.NewGuid();
        var sites = new List<WebSiteConfiguration>
        {
            new() { Id = siteId, Name = "ToRemove", ApplicationPath = "/volume1/web/remove" },
            new() { Id = Guid.NewGuid(), Name = "Keep", ApplicationPath = "/volume1/web/keep" }
        };
        SetupConfigWithSites(sites);
        var service = CreateService();

        // Act
        await service.RemoveSiteAsync(siteId);

        // Assert
        var loaded = await service.GetAllSitesAsync();
        var loadedList = loaded.ToList();
        Assert.Single(loadedList);
        Assert.Equal("Keep", loadedList[0].Name);
    }

    [Fact]
    public async Task RemoveSiteAsync_Throws_WhenNotFound()
    {
        // Arrange
        SetupEmptyConfig();
        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveSiteAsync(Guid.NewGuid()));
        Assert.Contains("not found", exception.Message);
    }

    #endregion
}

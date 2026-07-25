using Askyl.Dsm.WebHosting.Data.Domain.WebSites;

namespace Askyl.Dsm.WebHosting.Tests.Data.Domain.WebSites;

public class WebSiteInstanceTests
{
    #region Constructor

    [Fact]
    public void Constructor_Parameterless_SetsDefaultConfiguration()
    {
        // Act
        var instance = new WebSiteInstance();

        // Assert
        Assert.NotNull(instance.Configuration);
        Assert.Equal(Guid.Empty, instance.Id);
        Assert.False(instance.IsRunning);
    }

    [Fact]
    public void Constructor_WithConfiguration_SetsConfiguration()
    {
        // Arrange
        var config = new WebSiteConfiguration
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "TestSite"
        };

        // Act
        var instance = new WebSiteInstance(config);

        // Assert
        Assert.Same(config, instance.Configuration);
        Assert.Equal(config.Id, instance.Id);
    }

    #endregion

    #region Id Property

    [Fact]
    public void Id_ForwardsToConfigurationId()
    {
        // Arrange
        var config = new WebSiteConfiguration
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };
        var instance = new WebSiteInstance(config);

        // Act & Assert
        Assert.Equal(config.Id, instance.Id);
    }

    #endregion

    #region Process Property

    [Fact]
    public void Process_DefaultsToNull()
    {
        // Act
        var instance = new WebSiteInstance();

        // Assert
        Assert.Null(instance.Process);
    }

    [Fact]
    public void Process_CanBeSet()
    {
        // Arrange
        var instance = new WebSiteInstance();
        var processInfo = new ProcessInfo(1234);

        // Act
        instance.Process = processInfo;

        // Assert
        Assert.Same(processInfo, instance.Process);
    }

    #endregion
}

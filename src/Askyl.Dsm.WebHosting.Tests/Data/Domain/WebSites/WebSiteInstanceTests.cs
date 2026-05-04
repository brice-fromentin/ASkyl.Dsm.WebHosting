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

    #region State Property

    [Fact]
    public void State_WhenRunning_ReturnsRunning()
    {
        // Arrange
        var instance = new WebSiteInstance { IsRunning = true };

        // Act & Assert
        Assert.Equal("Running", instance.State);
    }

    [Fact]
    public void State_WhenStopped_ReturnsStopped()
    {
        // Arrange
        var instance = new WebSiteInstance { IsRunning = false };

        // Act & Assert
        Assert.Equal("Stopped", instance.State);
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

    #region WebSiteInstanceDetails

    [Fact]
    public void WebSiteInstanceDetails_InheritsFromWebSiteInstance()
    {
        // Arrange
        var config = new WebSiteConfiguration { Name = "TestSite" };

        // Act
        var details = new WebSiteInstanceDetails(config);

        // Assert
        Assert.IsAssignableFrom<WebSiteInstance>(details);
        Assert.Same(config, details.Configuration);
    }

    [Fact]
    public void WebSiteInstanceDetails_CanSetProcess()
    {
        // Act
        var details = new WebSiteInstanceDetails();
        details.Process = new ProcessInfo(1234);

        // Assert
        Assert.NotNull(details.Process);
        Assert.Equal(1234, details.Process!.Id);
    }

    [Fact]
    public void WebSiteInstanceDetails_Process_DefaultsToNull()
    {
        // Act
        var details = new WebSiteInstanceDetails();

        // Assert
        Assert.Null(details.Process);
    }

    #endregion

    #region WebSiteRuntimeState

    [Fact]
    public void WebSiteRuntimeState_Stopped_Factory_ReturnsCorrectState()
    {
        // Act
        var state = WebSiteRuntimeState.Stopped;

        // Assert
        Assert.False(state.IsRunning);
        Assert.Null(state.ProcessDetails);
        Assert.Equal("Stopped", state.StatusText);
    }

    [Fact]
    public void WebSiteRuntimeState_Running_Factory_ReturnsCorrectState()
    {
        // Arrange
        var processInfo = new ProcessInfo(5678);

        // Act
        var state = WebSiteRuntimeState.Running(processInfo);

        // Assert
        Assert.True(state.IsRunning);
        Assert.Same(processInfo, state.ProcessDetails);
        Assert.Equal("Running", state.StatusText);
    }

    #endregion
}

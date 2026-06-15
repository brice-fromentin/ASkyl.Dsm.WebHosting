using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Globalization.Validators;

namespace Askyl.Dsm.WebHosting.Tests.Data.Domain.WebSites;

public class WebSiteConfigurationTests
{
    private const int PortMin = 1024;
    private const int PortMax = 65535;
    private const int PublicPortWellKnownHttp = 80;
    private const int PublicPortWellKnownHttps = 443;
    private const int TimeoutMin = 10;
    private const int TimeoutMax = 120;
    private const int NameMaxLength = 100;

    private static WebSiteConfigurationValidator CreateValidator()
    {
        return new();
    }

    #region Name

    [Fact]
    public void Validate_Name_Empty_Fails()
    {
        // Arrange
        var config = new WebSiteConfiguration { Name = "" };
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.Name));
    }

    [Fact]
    public void Validate_Name_Null_Fails()
    {
        // Arrange
        var config = new WebSiteConfiguration { Name = null! };
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.Name));
    }

    [Fact]
    public void Validate_Name_Exceeds100Chars_Fails()
    {
        // Arrange
        var config = new WebSiteConfiguration { Name = new string('A', NameMaxLength + 1) };
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.Name));
    }

    [Fact]
    public void Validate_Name_Exact100Chars_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Name = new string('A', NameMaxLength);
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.Name));
    }

    [Fact]
    public void Validate_Name_Valid_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.Name));
    }

    #endregion

    #region ApplicationPath

    [Fact]
    public void Validate_ApplicationPath_Empty_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ApplicationPath = "";
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.ApplicationPath));
    }

    [Fact]
    public void Validate_ApplicationPath_Null_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ApplicationPath = null!;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.ApplicationPath));
    }

    [Fact]
    public void Validate_ApplicationPath_Valid_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.ApplicationPath));
    }

    #endregion

    #region InternalPort

    [Fact]
    public void Validate_InternalPort_Below1024_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InternalPort = PortMin - 1;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.InternalPort));
    }

    [Fact]
    public void Validate_InternalPort_Above65535_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InternalPort = PortMax + 1;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.InternalPort));
    }

    [Fact]
    public void Validate_InternalPort_MinBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InternalPort = PortMin;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.InternalPort));
    }

    [Fact]
    public void Validate_InternalPort_MaxBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InternalPort = PortMax;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.InternalPort));
    }

    #endregion

    #region HostName

    [Fact]
    public void Validate_HostName_Empty_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.HostName = "";
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.HostName));
    }

    [Fact]
    public void Validate_HostName_Null_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.HostName = null!;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.HostName));
    }

    [Fact]
    public void Validate_HostName_Valid_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.HostName));
    }

    #endregion

    #region Environment

    [Fact]
    public void Validate_Environment_Empty_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Environment = "";
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.Environment));
    }

    [Fact]
    public void Validate_Environment_Null_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Environment = null!;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.Environment));
    }

    [Fact]
    public void Validate_Environment_Valid_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.Environment));
    }

    #endregion

    #region ProcessTimeoutSeconds

    [Fact]
    public void Validate_ProcessTimeoutSeconds_BelowMin_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ProcessTimeoutSeconds = TimeoutMin - 1;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.ProcessTimeoutSeconds));
    }

    [Fact]
    public void Validate_ProcessTimeoutSeconds_AboveMax_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ProcessTimeoutSeconds = TimeoutMax + 1;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.ProcessTimeoutSeconds));
    }

    [Fact]
    public void Validate_ProcessTimeoutSeconds_MinBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ProcessTimeoutSeconds = TimeoutMin;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.ProcessTimeoutSeconds));
    }

    [Fact]
    public void Validate_ProcessTimeoutSeconds_MaxBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ProcessTimeoutSeconds = TimeoutMax;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.ProcessTimeoutSeconds));
    }

    #endregion

    #region PublicPort

    [Fact]
    public void Validate_PublicPort_Zero_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = 0;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    [Fact]
    public void Validate_PublicPort_Negative_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = -1;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    [Fact]
    public void Validate_PublicPort_WellKnownHttp_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = PublicPortWellKnownHttp;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    [Fact]
    public void Validate_PublicPort_WellKnownHttps_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = PublicPortWellKnownHttps;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    [Fact]
    public void Validate_PublicPort_ValidHighPort_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = PortMin;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    [Fact]
    public void Validate_PublicPort_BelowWellKnown_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = 1;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    [Fact]
    public void Validate_PublicPort_BetweenWellKnownAndHighPort_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = 500; // Between well-known ports (80, 443) and high port range (1024+)
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    [Fact]
    public void Validate_PublicPort_AboveMax_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = PortMax + 1;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    [Fact]
    public void Validate_PublicPort_MinHighPortBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = PortMin;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    [Fact]
    public void Validate_PublicPort_MaxPortBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.PublicPort = PortMax;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(config);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(WebSiteConfiguration.PublicPort));
    }

    #endregion

    #region Helpers

    private static WebSiteConfiguration CreateValidConfig()
    {
        return new()
        {
            Name = "TestSite",
            ApplicationPath = "/path/to/app.dll",
            InternalPort = 5000,
            PublicPort = PublicPortWellKnownHttps,
            HostName = "example.com",
            Environment = "Production",
            ProcessTimeoutSeconds = 30
        };
    }

    #endregion
}

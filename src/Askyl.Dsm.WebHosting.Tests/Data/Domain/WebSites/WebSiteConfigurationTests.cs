using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;

namespace Askyl.Dsm.WebHosting.Tests.Data.Domain.WebSites;

public class WebSiteConfigurationTests
{
    private const int PortMin = 1024;
    private const int PortMax = 65535;
    private const int TimeoutMin = 10;
    private const int TimeoutMax = 120;
    private const int NameMaxLength = 100;
    #region Name

    [Fact]
    public void Validate_Name_Empty_Fails()
    {
        // Arrange
        var config = new WebSiteConfiguration { Name = "" };
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var nameErrors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.Name))).ToList();
        Assert.NotEmpty(nameErrors);
    }

    [Fact]
    public void Validate_Name_Null_Fails()
    {
        // Arrange
        var config = new WebSiteConfiguration { Name = null! };
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var nameErrors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.Name))).ToList();
        Assert.NotEmpty(nameErrors);
    }

    [Fact]
    public void Validate_Name_Exceeds100Chars_Fails()
    {
        // Arrange
        var config = new WebSiteConfiguration { Name = new string('A', NameMaxLength + 1) };
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var nameErrors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.Name))).ToList();
        Assert.NotEmpty(nameErrors);
    }

    [Fact]
    public void Validate_Name_Exact100Chars_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Name = new string('A', NameMaxLength);
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        var nameErrors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.Name))).ToList();
        Assert.Empty(nameErrors);
    }

    [Fact]
    public void Validate_Name_Valid_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        var nameErrors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.Name))).ToList();
        Assert.Empty(nameErrors);
    }

    #endregion

    #region ApplicationPath

    [Fact]
    public void Validate_ApplicationPath_Empty_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ApplicationPath = "";
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.ApplicationPath))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ApplicationPath_Null_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ApplicationPath = null!;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.ApplicationPath))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ApplicationPath_Valid_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.ApplicationPath))).ToList();
        Assert.Empty(errors);
    }

    #endregion

    #region InternalPort

    [Fact]
    public void Validate_InternalPort_Below1024_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InternalPort = PortMin - 1;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.InternalPort))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_InternalPort_Above65535_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InternalPort = PortMax + 1;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.InternalPort))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_InternalPort_MinBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InternalPort = PortMin;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.InternalPort))).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_InternalPort_MaxBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.InternalPort = PortMax;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.InternalPort))).ToList();
        Assert.Empty(errors);
    }

    #endregion

    #region HostName

    [Fact]
    public void Validate_HostName_Empty_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.HostName = "";
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.HostName))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_HostName_Null_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.HostName = null!;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.HostName))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_HostName_Valid_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.HostName))).ToList();
        Assert.Empty(errors);
    }

    #endregion

    #region Environment

    [Fact]
    public void Validate_Environment_Empty_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Environment = "";
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.Environment))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_Environment_Null_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.Environment = null!;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.Environment))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_Environment_Valid_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.Environment))).ToList();
        Assert.Empty(errors);
    }

    #endregion

    #region ProcessTimeoutSeconds

    [Fact]
    public void Validate_ProcessTimeoutSeconds_BelowMin_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ProcessTimeoutSeconds = TimeoutMin - 1;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.ProcessTimeoutSeconds))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ProcessTimeoutSeconds_AboveMax_Fails()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ProcessTimeoutSeconds = TimeoutMax + 1;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.ProcessTimeoutSeconds))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ProcessTimeoutSeconds_MinBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ProcessTimeoutSeconds = TimeoutMin;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.ProcessTimeoutSeconds))).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ProcessTimeoutSeconds_MaxBoundary_Passes()
    {
        // Arrange
        var config = CreateValidConfig();
        config.ProcessTimeoutSeconds = TimeoutMax;
        var context = new ValidationContext(config);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(config, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(WebSiteConfiguration.ProcessTimeoutSeconds))).ToList();
        Assert.Empty(errors);
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
            HostName = "example.com",
            Environment = "Production",
            ProcessTimeoutSeconds = 30
        };
    }

    #endregion
}

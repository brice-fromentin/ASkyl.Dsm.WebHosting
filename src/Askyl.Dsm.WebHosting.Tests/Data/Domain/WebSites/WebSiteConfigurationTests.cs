using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;

namespace Askyl.Dsm.WebHosting.Tests.Data.Domain.WebSites;

public class WebSiteConfigurationTests
{
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
        var config = new WebSiteConfiguration { Name = new string('A', 101) };
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
        config.Name = new string('A', 100);
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
        config.InternalPort = 1023;
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
        config.InternalPort = 65536;
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
        config.InternalPort = 1024;
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
        config.InternalPort = 65535;
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
        config.ProcessTimeoutSeconds = 9;
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
        config.ProcessTimeoutSeconds = 121;
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
        config.ProcessTimeoutSeconds = 10;
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
        config.ProcessTimeoutSeconds = 120;
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

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;

namespace Askyl.Dsm.WebHosting.Tests.Data.Domain.Authentication;

public class LoginCredentialsTests
{
    #region Login

    [Fact]
    public void Validate_Login_Empty_Fails()
    {
        // Arrange
        var credentials = new LoginCredentials("", "password", null);
        var context = new ValidationContext(credentials);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(credentials, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(LoginCredentials.Login))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_Login_Null_Fails()
    {
        // Arrange
        var credentials = new LoginCredentials();
        credentials.Login = null!;
        var context = new ValidationContext(credentials);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(credentials, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(LoginCredentials.Login))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_Login_Valid_Passes()
    {
        // Arrange
        var credentials = CreateValidCredentials();
        var context = new ValidationContext(credentials);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(credentials, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(LoginCredentials.Login))).ToList();
        Assert.Empty(errors);
    }

    #endregion

    #region Password

    [Fact]
    public void Validate_Password_Empty_Fails()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "", null);
        var context = new ValidationContext(credentials);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(credentials, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(LoginCredentials.Password))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_Password_Null_Fails()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "password", null);
        credentials.Password = null!;
        var context = new ValidationContext(credentials);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(credentials, context, results, validateAllProperties: true);

        // Assert
        Assert.False(isValid);
        var errors = results.Where(r => r.MemberNames.Contains(nameof(LoginCredentials.Password))).ToList();
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_Password_Valid_Passes()
    {
        // Arrange
        var credentials = CreateValidCredentials();
        var context = new ValidationContext(credentials);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(credentials, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(LoginCredentials.Password))).ToList();
        Assert.Empty(errors);
    }

    #endregion

    #region OtpCode

    [Fact]
    public void Validate_OtpCode_Null_Passes()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "password", null);
        var context = new ValidationContext(credentials);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(credentials, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(LoginCredentials.OtpCode))).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_OtpCode_Empty_Passes()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "password", "");
        var context = new ValidationContext(credentials);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(credentials, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(LoginCredentials.OtpCode))).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_OtpCode_Valid_Passes()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "password", "123456");
        var context = new ValidationContext(credentials);
        var results = new List<ValidationResult>();

        // Act
        Validator.TryValidateObject(credentials, context, results, validateAllProperties: true);

        // Assert
        var errors = results.Where(r => r.MemberNames.Contains(nameof(LoginCredentials.OtpCode))).ToList();
        Assert.Empty(errors);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        var login = "admin";
        var password = "secret";
        var otp = "123456";

        // Act
        var credentials = new LoginCredentials(login, password, otp);

        // Assert
        Assert.Equal(login, credentials.Login);
        Assert.Equal(password, credentials.Password);
        Assert.Equal(otp, credentials.OtpCode);
    }

    [Fact]
    public void Constructor_Parameterless_SetsDefaults()
    {
        // Act
        var credentials = new LoginCredentials();

        // Assert
        Assert.Equal("", credentials.Login);
        Assert.Equal("", credentials.Password);
        Assert.Null(credentials.OtpCode);
    }

    #endregion

    #region Helpers

    private static LoginCredentials CreateValidCredentials()
    {
        return new("admin", "password", null);
    }

    #endregion
}

using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Validators;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Data.Domain.Authentication;

public class LoginCredentialsTests
{
    private static LoginCredentialsValidator CreateValidator()
    {
        var localizerMock = new Mock<ILocalizer>();
        localizerMock.Setup(x => x[It.IsAny<string>(), It.IsAny<object[]>()])
                     .Returns((string name, object[]? args) => new LocalizedText(name, name));
        return new LoginCredentialsValidator(localizerMock.Object);
    }

    #region Login

    [Fact]
    public void Validate_Login_Empty_Fails()
    {
        // Arrange
        var credentials = new LoginCredentials("", "password", null);
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(credentials);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCredentials.Login));
    }

    [Fact]
    public void Validate_Login_Null_Fails()
    {
        // Arrange
        var credentials = new LoginCredentials();
        credentials.Login = null!;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(credentials);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCredentials.Login));
    }

    [Fact]
    public void Validate_Login_Valid_Passes()
    {
        // Arrange
        var credentials = CreateValidCredentials();
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(credentials);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(LoginCredentials.Login));
    }

    #endregion

    #region Password

    [Fact]
    public void Validate_Password_Empty_Fails()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "", null);
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(credentials);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCredentials.Password));
    }

    [Fact]
    public void Validate_Password_Null_Fails()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "password", null);
        credentials.Password = null!;
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(credentials);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCredentials.Password));
    }

    [Fact]
    public void Validate_Password_Valid_Passes()
    {
        // Arrange
        var credentials = CreateValidCredentials();
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(credentials);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(LoginCredentials.Password));
    }

    #endregion

    #region OtpCode

    [Fact]
    public void Validate_OtpCode_Null_Passes()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "password", null);
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(credentials);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(LoginCredentials.OtpCode));
    }

    [Fact]
    public void Validate_OtpCode_Empty_Passes()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "password", "");
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(credentials);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(LoginCredentials.OtpCode));
    }

    [Fact]
    public void Validate_OtpCode_Valid_Passes()
    {
        // Arrange
        var credentials = new LoginCredentials("admin", "password", "123456");
        var validator = CreateValidator();

        // Act
        var result = validator.Validate(credentials);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(LoginCredentials.OtpCode));
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        // Arrange
        const string login = "admin";
        const string password = "secret";
        const string otp = "123456";

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

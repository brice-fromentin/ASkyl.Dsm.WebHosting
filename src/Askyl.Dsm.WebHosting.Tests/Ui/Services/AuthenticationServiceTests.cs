using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Validators;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class AuthenticationServiceTests
{
    readonly Mock<IDsmSession> _dsmSession;
    readonly Mock<ILogger<ILogAuthenticationService>> _logger;
    readonly Mock<ILocalizer> _localizer;

    public AuthenticationServiceTests()
    {
        _dsmSession = new Mock<IDsmSession>();
        _logger = new Mock<ILogger<ILogAuthenticationService>>();
        _localizer = new Mock<ILocalizer>();
        _localizer.Setup(l => l[LK.Error.AuthenticationFailed]).Returns("Authentication failed");
        _localizer.Setup(l => l[LK.Error.AdministratorRequired]).Returns("Administrators only");
        _localizer.Setup(l => l[LK.Success.LogoutSuccessful]).Returns("Logout successful");
        _localizer.Setup(l => l[LK.Error.SessionExpired]).Returns("Session expired");
    }

    AuthenticationService CreateService()
    {
        return new AuthenticationService(_dsmSession.Object, new LoginCredentialsValidator(), _logger.Object, _localizer.Object);
    }

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_ReturnsSuccess_WithValidCredentials()
    {
        // Arrange
        _dsmSession.Setup(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult.CreateSuccess());
        _dsmSession.SetupGet(s => s.UserLanguage).Returns("enu");
        _dsmSession.SetupGet(s => s.UserDateFormat).Returns(null as string);
        _dsmSession.SetupGet(s => s.UserTimeFormat).Returns(null as string);

        var service = CreateService();

        // Act
        var result = await service.LoginAsync("admin", "password123", null);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IsAuthenticated);
        _dsmSession.Verify(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_RejectsEmptyCredentials_WithoutContactingDsm()
    {
        // Arrange — validation replaced the model-binding filter, so the service itself must refuse
        // input the shared validator rejects, before any DSM call is made.
        var service = CreateService();

        // Act
        var result = await service.LoginAsync(String.Empty, String.Empty, null);

        // Assert
        Assert.False(result.Success);
        Assert.False(String.IsNullOrWhiteSpace(result.Message));
        _dsmSession.Verify(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailure_WithInvalidCredentials()
    {
        // Arrange
        _dsmSession.Setup(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResult(false, null, ApiErrorCode.Unauthorized));

        var service = CreateService();

        // Act
        var result = await service.LoginAsync("admin", "wrongpassword", null);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.IsAuthenticated);
        Assert.Equal("Authentication failed", result.Message);
    }

    [Fact]
    public async Task LoginAsync_ReturnsAdministratorMessage_WhenUserIsNotAdministrator()
    {
        // Arrange — credentials are valid but the user lacks administrator rights, which must
        // surface a distinct message rather than the generic authentication failure.
        _dsmSession.Setup(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResult(false, null, ApiErrorCode.Forbidden));

        var service = CreateService();

        // Act
        var result = await service.LoginAsync("guest", "password123", null);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.IsAuthenticated);
        Assert.Equal("Administrators only", result.Message);
    }

    [Fact]
    public async Task LoginAsync_PassesOtpCode_WhenProvided()
    {
        // Arrange
        var capturedCredentials = default(LoginCredentials);
        _dsmSession.Setup(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()))
            .Returns((LoginCredentials credentials, CancellationToken ct) =>
            {
                capturedCredentials = credentials;
                return Task.FromResult(ApiResult.CreateSuccess());
            });
        _dsmSession.SetupGet(s => s.UserLanguage).Returns(null as string);

        var service = CreateService();

        // Act
        await service.LoginAsync("admin", "password", "123456");

        // Assert
        Assert.NotNull(capturedCredentials);
        Assert.Equal("admin", capturedCredentials!.Login);
        Assert.Equal("password", capturedCredentials.Password);
        Assert.Equal("123456", capturedCredentials.OtpCode);
    }

    #endregion

    #region LogoutAsync

    [Fact]
    public async Task LogoutAsync_ClearsSession()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.LogoutAsync();

        // Assert
        Assert.True(result.Success);
        _dsmSession.Verify(s => s.Disconnect(), Times.Once);
    }

    #endregion

    #region IsAuthenticatedAsync

    [Fact]
    public async Task IsAuthenticatedAsync_ReturnsTrue_WhenSessionValid()
    {
        // Arrange
        _dsmSession.Setup(s => s.ValidateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.IsAuthenticatedAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Value);
        _dsmSession.Verify(s => s.Disconnect(), Times.Never);
    }

    [Fact]
    public async Task IsAuthenticatedAsync_ReturnsFalse_WhenSessionInvalid()
    {
        // Arrange
        _dsmSession.Setup(s => s.ValidateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.IsAuthenticatedAsync();

        // Assert
        Assert.True(result.Success);
        Assert.False(result.Value);
        Assert.Equal("Session expired", result.Message);
        _dsmSession.Verify(s => s.Disconnect(), Times.Once);
    }

    #endregion
}

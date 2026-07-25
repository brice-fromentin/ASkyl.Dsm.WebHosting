using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Globalization;
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
        _localizer.Setup(l => l[LK.Success.LogoutSuccessful]).Returns("Logout successful");
        _localizer.Setup(l => l[LK.Error.SessionExpired]).Returns("Session expired");
    }

    AuthenticationService CreateService()
    {
        return new AuthenticationService(_dsmSession.Object, _logger.Object, _localizer.Object);
    }

    #region LoginAsync

    [Fact]
    public async Task LoginAsync_ReturnsSuccess_WithValidCredentials()
    {
        // Arrange
        _dsmSession.Setup(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
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
    public async Task LoginAsync_ReturnsFailure_WithInvalidCredentials()
    {
        // Arrange
        _dsmSession.Setup(s => s.ConnectAsync(It.IsAny<LoginCredentials>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.LoginAsync("admin", "wrongpassword", null);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.IsAuthenticated);
        Assert.Equal("Authentication failed", result.Message);
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
                return Task.FromResult(true);
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

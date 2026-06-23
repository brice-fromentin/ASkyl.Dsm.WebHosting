using Askyl.Dsm.WebHosting.Constants.DSM.API;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.WebSites;
using Askyl.Dsm.WebHosting.Data.DsmApi.Models.ReverseProxy;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters;
using Askyl.Dsm.WebHosting.Data.DsmApi.Parameters.Core.AppPortal.ReverseProxy;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses;
using Askyl.Dsm.WebHosting.Data.DsmApi.Responses.Core.AppPortal.ReverseProxy;
using Askyl.Dsm.WebHosting.Data.Exceptions;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Ui.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

/// <summary>
/// Regression tests verifying ReverseProxyManagerService correctly delegates to IDsmSession.
/// Ensures the refactored session architecture (P5/P6) is working correctly.
/// </summary>
public class ReverseProxyManagerServiceTests
{
    readonly Mock<IDsmSession> _dsmSession;
    readonly Mock<ILogger<ILogReverseProxyManagerService>> _logger;

    public ReverseProxyManagerServiceTests()
    {
        _dsmSession = new Mock<IDsmSession>();
        _logger = new Mock<ILogger<ILogReverseProxyManagerService>>();
    }

    ReverseProxyManagerService CreateService()
    {
        return new ReverseProxyManagerService(_logger.Object, _dsmSession.Object);
    }

    WebSiteConfiguration CreateSite(string name = "TestSite")
    {
        return new WebSiteConfiguration
        {
            Id = Guid.NewGuid(),
            Name = name,
            ApplicationPath = "/volume1/web/test",
            ApplicationRealPath = "/volume1/web/test/app.dll",
            InternalPort = 5001,
            PublicPort = 443,
            HostName = "test.local",
            Protocol = ProtocolType.HTTPS,
            EnableHSTS = false
        };
    }

    ReverseProxy CreateProxy(Guid? uuid = null)
    {
        return new ReverseProxy
        {
            UUID = uuid ?? Guid.NewGuid(),
            Description = "ADWH:TestSite",
            Frontend = new ReverseProxyFrontend("test.local", 443, (int)ProtocolType.HTTPS, new ReverseProxyHttps(false)),
            Backend = new ReverseProxyBackend(NetworkConstants.Localhost, 5001, (int)ProtocolType.HTTP)
        };
    }

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_NewProxy_CreatesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var site = CreateSite();
        var createdProxy = CreateProxy(uuid: Guid.NewGuid());
        var listCallCount = 0;

        _dsmSession
            .Setup(s => s.ExecuteAsync<ReverseProxyListResponse>(It.IsAny<IApiParameters>()))
            .ReturnsAsync(() =>
            {
                var count = Interlocked.Increment(ref listCallCount);

                if (count == 1)
                {
                    return new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [] } };
                }

                return new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [createdProxy] } };
            });

        _dsmSession
            .Setup(s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyCreateParameters>()))
            .ReturnsAsync(new ApiResponseBase<object> { Success = true });

        // Act
        await service.CreateAsync(site);

        // Assert
        _dsmSession.Verify(
            s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyCreateParameters>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AlreadyExists_IsIdempotent()
    {
        // Arrange
        var service = CreateService();
        var site = CreateSite();
        var existingProxy = CreateProxy(uuid: Guid.NewGuid());

        _dsmSession
            .Setup(s => s.ExecuteAsync<ReverseProxyListResponse>(It.IsAny<IApiParameters>()))
            .ReturnsAsync(new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [existingProxy] } });

        // Act
        await service.CreateAsync(site);

        // Assert - no create call should be made
        _dsmSession.Verify(
            s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyCreateParameters>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ThrowsOnApiFailure()
    {
        // Arrange
        var service = CreateService();
        var site = CreateSite();
        var listCallCount = 0;

        _dsmSession
            .Setup(s => s.ExecuteAsync<ReverseProxyListResponse>(It.IsAny<IApiParameters>()))
            .ReturnsAsync(() =>
            {
                var count = Interlocked.Increment(ref listCallCount);

                if (count == 1)
                {
                    return new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [] } };
                }

                return new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [] } };
            });

        _dsmSession
            .Setup(s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyCreateParameters>()))
            .ReturnsAsync(new ApiResponseBase<object> { Success = false, Error = new ApiError { Code = -1 } });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(site));
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ExistingProxy_UpdatesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var site = CreateSite();
        var existingProxy = CreateProxy(uuid: Guid.NewGuid());

        _dsmSession
            .Setup(s => s.ExecuteAsync<ReverseProxyListResponse>(It.IsAny<IApiParameters>()))
            .ReturnsAsync(new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [existingProxy] } });

        _dsmSession
            .Setup(s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyUpdateParameters>()))
            .ReturnsAsync(new ApiResponseBase<object> { Success = true });

        // Act
        await service.UpdateAsync(site);

        // Assert
        _dsmSession.Verify(
            s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyUpdateParameters>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ThrowsException()
    {
        // Arrange
        var service = CreateService();
        var site = CreateSite();

        _dsmSession
            .Setup(s => s.ExecuteAsync<ReverseProxyListResponse>(It.IsAny<IApiParameters>()))
            .ReturnsAsync(new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [] } });

        // Act & Assert
        await Assert.ThrowsAsync<ReverseProxyNotFoundException>(() => service.UpdateAsync(site));
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ExistingProxy_DeletesSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var site = CreateSite();
        var proxyUuid = Guid.NewGuid();
        var existingProxy = CreateProxy(uuid: proxyUuid);

        _dsmSession
            .Setup(s => s.ExecuteAsync<ReverseProxyListResponse>(It.IsAny<IApiParameters>()))
            .ReturnsAsync(new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [existingProxy] } });

        _dsmSession
            .Setup(s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyDeleteParameters>()))
            .ReturnsAsync(new ApiResponseBase<object> { Success = true });

        // Act
        await service.DeleteAsync(site);

        // Assert
        _dsmSession.Verify(
            s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyDeleteParameters>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_IsGraceful()
    {
        // Arrange
        var service = CreateService();
        var site = CreateSite();

        _dsmSession
            .Setup(s => s.ExecuteAsync<ReverseProxyListResponse>(It.IsAny<IApiParameters>()))
            .ReturnsAsync(new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [] } });

        // Act - should not throw
        await service.DeleteAsync(site);

        // Assert - no delete call should be made
        _dsmSession.Verify(
            s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyDeleteParameters>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeletedExternally_IsGraceful()
    {
        // Arrange
        var service = CreateService();
        var site = CreateSite();
        var proxyUuid = Guid.NewGuid();
        var existingProxy = CreateProxy(uuid: proxyUuid);

        _dsmSession
            .Setup(s => s.ExecuteAsync<ReverseProxyListResponse>(It.IsAny<IApiParameters>()))
            .ReturnsAsync(new ReverseProxyListResponse { Success = true, Data = new ReverseProxyList { Entries = [existingProxy] } });

        _dsmSession
            .Setup(s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyDeleteParameters>()))
            .ReturnsAsync(new ApiResponseBase<object>
            {
                Success = false,
                Error = new ApiError { Code = ReverseProxyConstants.ErrorCodeNotFound }
            });

        // Act - should not throw
        await service.DeleteAsync(site);

        // Assert - delete was attempted
        _dsmSession.Verify(
            s => s.ExecuteSimpleAsync(It.IsAny<ReverseProxyDeleteParameters>()),
            Times.Once);
    }

    #endregion
}

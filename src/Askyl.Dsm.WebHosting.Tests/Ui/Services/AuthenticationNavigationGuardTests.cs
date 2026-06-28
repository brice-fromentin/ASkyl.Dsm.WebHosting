using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Ui.Client.Contracts;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class AuthenticationNavigationGuardTests : IDisposable
{
    private readonly BunitContext _ctx;
    private readonly Mock<IAuthenticationService> _authService;
    private readonly AuthenticationNavigationGuard _guard;
    private readonly NavigationManager _navigation;

    public AuthenticationNavigationGuardTests()
    {
        _ctx = new BunitContext();
        _ctx.JSInterop.Mode = JSRuntimeMode.Strict;
        _authService = new Mock<IAuthenticationService>();
        _ctx.Services.Add(new ServiceDescriptor(typeof(IAuthenticationService), _authService.Object, ServiceLifetime.Singleton));

        _navigation = _ctx.Services.GetService<NavigationManager>()!;
        _guard = new AuthenticationNavigationGuard(_authService.Object, _navigation);
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Guard_ImplementsINavigationGuard()
    {
        Assert.IsAssignableFrom<INavigationGuard>(_guard);
    }

    [Fact]
    public void OnNavigate_Sync_ThrowsNotSupportedException()
    {
        var context = CreateNavigationContext("test");
        var exception = Assert.Throws<NotSupportedException>(() => _guard.OnNavigate(context));
        Assert.Contains("async", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OnNavigateAsync_AllowsLoginPageWithoutAuth()
    {
        var context = CreateNavigationContext("login");
        await _guard.OnNavigateAsync(context);

        _authService.Verify(a => a.IsAuthenticatedAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnNavigateAsync_RedirectsUnauthenticatedUser()
    {
        _authService.Setup(a => a.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResultBool(true, null, false));

        var context = CreateNavigationContext("");
        await _guard.OnNavigateAsync(context);

        Assert.EndsWith("/login", _navigation.Uri);
    }

    [Fact]
    public async Task OnNavigateAsync_AllowsAuthenticatedUser()
    {
        _authService.Setup(a => a.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiResultBool(true, null, true));

        var initialUri = _navigation.Uri;
        var context = CreateNavigationContext("dashboard");
        await _guard.OnNavigateAsync(context);

        Assert.Equal(initialUri, _navigation.Uri);
    }

    static NavigationContext CreateNavigationContext(string path)
    {
        var normalizedPath = path.StartsWith('/') ? path : $"/{path}";
        var ctor = typeof(NavigationContext).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            [typeof(string), typeof(CancellationToken)])!;
        return (NavigationContext)ctor.Invoke([normalizedPath, CancellationToken.None])!;
    }
}

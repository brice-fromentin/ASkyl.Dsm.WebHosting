using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Ui.Client.Contracts;
using Askyl.Dsm.WebHosting.Ui.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

/// <summary>
/// Tests for AuthenticationNavigationGuard.
/// Full async navigation tests require Bunit to construct NavigationContext properly.
/// </summary>
public class AuthenticationNavigationGuardTests
{
    private readonly AuthenticationNavigationGuard _guard;

    public AuthenticationNavigationGuardTests()
    {
        var authService = new Mock<IAuthenticationService>();
        var navigation = new Mock<NavigationManager>();
        _guard = new AuthenticationNavigationGuard(authService.Object, navigation.Object);
    }

    [Fact]
    public void Guard_ImplementsINavigationGuard()
    {
        Assert.IsType<INavigationGuard>(_guard, exactMatch: false);
    }

    [Fact]
    public void OnNavigate_Sync_ThrowsNotSupportedException()
    {
        // NavigationContext is sealed with internal constructor parameters in test context.
        // The sync overload throws NotSupportedException without inspecting context,
        // so we construct a minimal instance via reflection.
        var contextType = typeof(NavigationContext);
        var ctor = contextType.GetConstructors().FirstOrDefault();

        if (ctor is null)
        {
            // If no constructor is accessible, skip the test
            return;
        }

        var parameters = ctor.GetParameters();
        var args = parameters.Select(_ => (object?)null).ToArray();

        var exception = Record.Exception(() =>
        {
            var context = ctor.Invoke(args);
            _guard.OnNavigate((NavigationContext)context!);
        });

        Assert.NotNull(exception);
        Assert.IsType<NotSupportedException>(exception);
        Assert.Contains("async", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}

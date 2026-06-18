using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Ui.Client.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Intercepts Blazor navigation to enforce authentication before any component renders.
/// Prevents the flash of protected content when user is not authenticated.
/// </summary>
public class AuthenticationNavigationGuard(IAuthenticationService authService, NavigationManager navigation) : INavigationGuard
{
    /// <summary>
    /// Called by the Blazor Router before any component renders for the target URL.
    /// </summary>
    public async Task OnNavigateAsync(NavigationContext context)
    {
        var path = context.Path.TrimStart('/');

        // Allow login page without auth check
        if (String.Equals(path, ApplicationConstants.LoginPagePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var result = await authService.IsAuthenticatedAsync();

        if (!(result.Success && result.Value == true))
        {
            navigation.NavigateTo(ApplicationConstants.LoginPagePath, forceLoad: false);
        }
    }

    /// <summary>
    /// Synchronous navigation is not supported — authentication requires an async HTTP call.
    /// </summary>
    public void OnNavigate(NavigationContext _)
    {
        throw new NotSupportedException("Synchronous navigation cannot enforce authentication. Use OnNavigateAsync instead.");
    }
}

using Microsoft.AspNetCore.Components.Routing;

namespace Askyl.Dsm.WebHosting.Ui.Client.Contracts;

/// <summary>
/// Intercepts Blazor navigation to enforce authentication before any component renders.
/// </summary>
public interface INavigationGuard
{
    /// <summary>
    /// Called by the Blazor Router before any component renders for the target URL.
    /// </summary>
    /// <param name="context">Navigation context containing the target path.</param>
    Task OnNavigateAsync(NavigationContext context);

    /// <summary>
    /// Synchronous navigation is not supported — authentication requires an async HTTP call.
    /// </summary>
    /// <param name="context">Navigation context containing the target path.</param>
    /// <exception cref="NotSupportedException">Always thrown — async navigation is required.</exception>
    void OnNavigate(NavigationContext context);
}

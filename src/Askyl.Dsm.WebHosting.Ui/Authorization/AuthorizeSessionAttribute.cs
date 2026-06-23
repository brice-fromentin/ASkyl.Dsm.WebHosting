using Askyl.Dsm.WebHosting.Data.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Askyl.Dsm.WebHosting.Ui.Authorization;

/// <summary>
/// Authorizes access only if the user has an active server-side session.
/// Validates against the DSM server to detect sessions that expired or were revoked outside the application.
/// Validation results are cached (TTL: 1 minute) to avoid per-request API overhead.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeSessionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Validate session against DSM server (with caching)
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationService>();
        var result = await authService.IsAuthenticatedAsync();

        if (result.Value != true)
        {
            context.Result = new ForbidResult();
        }
    }
}

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
        var result = await authService.IsAuthenticatedAsync(context.HttpContext.RequestAborted);

        if (result.Value != true)
        {
            // Unauthorized, not Forbid: ForbidResult asks the authentication middleware to challenge,
            // and Program.cs registers no scheme, so it threw and every refusal surfaced as a 500.
            // 401 is also the honest code — the session is missing or no longer valid, which is a
            // question of identity rather than of privilege.
            context.Result = new UnauthorizedResult();
        }
    }
}

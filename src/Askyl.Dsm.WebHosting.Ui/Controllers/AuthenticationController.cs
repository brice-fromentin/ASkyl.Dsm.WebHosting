using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

/// <summary>
/// Controller for authentication-related operations.
/// </summary>
[ApiController]
[Route(AuthenticationRoutes.ControllerBaseRoute)]
public class AuthenticationController(IAuthenticationService authService) : ControllerBase
{
    /// <summary>
    /// Checks if the current session is authenticated.
    /// </summary>
    /// <returns>True if authenticated, false otherwise.</returns>
    [HttpGet(AuthenticationRoutes.StatusRoute)]
    public async Task<ActionResult<bool>> IsAuthenticatedAsync(CancellationToken cancellationToken)
        => Ok(await authService.IsAuthenticatedAsync(cancellationToken));

    /// <summary>
    /// Attempts to authenticate the user with provided credentials.
    /// Stores DSM SID in server-side session for persistence.
    /// </summary>
    /// <param name="model">The login model containing login, password, and optional OTP code.</param>
    /// <returns>OK with authentication result (Success=true or Success=false with ErrorMessage).</returns>
    [HttpPost(AuthenticationRoutes.LoginRoute)]
    [EnableRateLimiting(ApplicationConstants.RateLimitPolicyLogin)]
    public async Task<ActionResult<AuthenticationResult>> Login([FromBody] LoginCredentials model, CancellationToken cancellationToken)
        => Ok(await authService.LoginAsync(model.Login, model.Password, model.OtpCode, cancellationToken));

    /// <summary>
    /// Logs out the current user and clears server-side session.
    /// </summary>
    /// <returns>OK with ApiResult indicating successful logout.</returns>
    [HttpPost(AuthenticationRoutes.LogoutRoute)]
    public async Task<ActionResult<ApiResult>> LogoutAsync(CancellationToken cancellationToken)
        => Ok(await authService.LogoutAsync(cancellationToken));
}

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Network;
using Microsoft.AspNetCore.Http;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Implementation of IAuthenticationService that wraps DsmApiClient.
/// </summary>
/// <param name="apiClient">The DSM API client for making authentication calls.</param>
/// <param name="httpContextAccessor">Access to current HTTP context for session management.</param>
/// <param name="logger">Logger for tracking authentication operations.</param>
public class AuthenticationService(DsmApiClient apiClient, IHttpContextAccessor httpContextAccessor, ILogger<ILogAuthenticationService> logger) : IAuthenticationService
{
    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(string login, string password, string? otpCode)
    {
        using var timer = new OperationTimer(elapsed => logger.LoginDuration(elapsed, login));

        logger.LoginStarting(login);

        var model = new LoginCredentials(login, password, otpCode);

        if (!await apiClient.ConnectAsync(model))
        {
            logger.LoginFailed(login);
            return AuthenticationResult.CreateNotAuthenticated("Invalid credentials");
        }

        // Store DSM _sid in server-side session for persistence
        httpContextAccessor.HttpContext?.Session.SetString(ApplicationConstants.DsmSessionKey, apiClient.Sid);
        logger.LoginSuccessful(login);
        return AuthenticationResult.CreateAuthenticated();
    }

    /// <inheritdoc/>
    public async Task<ApiResult> LogoutAsync()
    {
        using var timer = new OperationTimer(elapsed => logger.LogoutDuration(elapsed));

        logger.LogoutStarting();

        try
        {
            httpContextAccessor.HttpContext?.Session.Remove(ApplicationConstants.DsmSessionKey);
            await apiClient.DisconnectAsync();
            logger.UserLoggedOut();
            return ApiResult.CreateSuccess("Logout successful");
        }
        catch (Exception ex)
        {
            logger.LogoutError(ex);
            return ApiResult.CreateFailure($"Logout failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Task<ApiResultBool> IsAuthenticatedAsync()
    {
        var sid = httpContextAccessor.HttpContext?.Session.GetString(ApplicationConstants.DsmSessionKey);

        return Task.FromResult(!String.IsNullOrEmpty(sid) ? ApiResultBool.CreateSuccess(true) : ApiResultBool.CreateSuccess(false, "No session found"));
    }
}

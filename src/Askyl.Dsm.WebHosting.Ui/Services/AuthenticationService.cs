using Microsoft.AspNetCore.Http;

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Tools.Network;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// Implementation of IAuthenticationService that wraps DsmApiClient.
/// </summary>
/// <param name="apiClient">The DSM API client for making authentication calls.</param>
/// <param name="httpContextAccessor">Access to current HTTP context for session management.</param>
/// <param name="logger">Logger for tracking authentication operations.</param>
public class AuthenticationService(DsmApiClient apiClient, IHttpContextAccessor httpContextAccessor, ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(string login, string password, string? otpCode)
    {
        var model = new LoginCredentials(login, password, otpCode);

        if (!await apiClient.ConnectAsync(model))
        {
            logger.LogWarning("Login failed for user: {Login}", login);
            return AuthenticationResult.CreateNotAuthenticated("Invalid credentials");
        }

        // Store DSM _sid in server-side session for persistence
        _httpContextAccessor.HttpContext?.Session.SetString(ApplicationConstants.DsmSessionKey, apiClient.Sid);
        logger.LogInformation("Login successful for user: {Login} - SID stored", login);
        return AuthenticationResult.CreateAuthenticated();
    }

    /// <inheritdoc/>
    public async Task<ApiResult> LogoutAsync()
    {
        try
        {
            _httpContextAccessor.HttpContext?.Session.Remove(ApplicationConstants.DsmSessionKey);
            await apiClient.DisconnectAsync();
            logger.LogInformation("User logged out");
            return ApiResult.CreateSuccess("Logout successful");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during logout");
            return ApiResult.CreateFailure($"Logout failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Task<ApiResultBool> IsAuthenticatedAsync()
    {
        var sid = _httpContextAccessor.HttpContext?.Session.GetString(ApplicationConstants.DsmSessionKey);

        return Task.FromResult(!String.IsNullOrEmpty(sid) ? ApiResultBool.CreateSuccess(true): ApiResultBool.CreateSuccess(false, "No session found"));
    }
}

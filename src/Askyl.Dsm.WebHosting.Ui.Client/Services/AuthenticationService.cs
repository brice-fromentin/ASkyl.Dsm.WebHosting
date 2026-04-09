using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Proxy implementation of IAuthenticationService that calls REST API endpoints.
/// Singleton service for the app lifetime. Authentication is managed server-side
/// via ASP.NET Core session (HttpOnly cookie), not client-side token storage.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
public class AuthenticationService(IHttpClientFactory httpClientFactory) : IAuthenticationService
{
    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(string login, string password, string? otpCode)
    {
        var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
        return await httpClient.PostJsonOrDefaultAsync<LoginCredentials, AuthenticationResult>(AuthenticationRoutes.LoginFullRoute, new(login, password, otpCode), () => AuthenticationResult.CreateNotAuthenticated("Failed to login"));
    }

    /// <inheritdoc/>
    public async Task<ApiResult> LogoutAsync()
    {
        var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
        return await httpClient.PostJsonOrDefaultAsync<object, ApiResult>(AuthenticationRoutes.LogoutFullRoute, null, () => ApiResult.CreateFailure("Unknown error"));
    }

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsAuthenticatedAsync()
    {
        var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
        return await httpClient.GetJsonOrDefaultAsync<ApiResultBool>(AuthenticationRoutes.StatusFullRoute, () => ApiResultBool.CreateFailure("Failed to check authentication status"));
    }
}

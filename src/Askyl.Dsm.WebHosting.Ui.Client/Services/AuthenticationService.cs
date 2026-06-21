using System.Net;
using System.Text;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.JSON;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Domain.Authentication;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Tools.Extensions;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Proxy implementation of IAuthenticationService that calls REST API endpoints.
/// Singleton service for the app lifetime. Authentication is managed server-side
/// via ASP.NET Core session (HttpOnly cookie), not client-side token storage.
/// </summary>
/// <param name="httpClientFactory">HttpClientFactory to create the named client.</param>
/// <param name="localizer">Localizer for user-facing strings.</param>
public class AuthenticationService(IHttpClientFactory httpClientFactory, ILocalizer localizer) : IAuthenticationService
{
    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(string login, string password, string? otpCode)
    {
        var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);

        var jsonContent = new StringContent(JsonSerializer.Serialize(new LoginCredentials(login, password, otpCode), JsonOptionsCache.Options), Encoding.UTF8, NetworkConstants.ApplicationJson);

        var response = await httpClient.PostAsync(AuthenticationRoutes.LoginFullRoute, jsonContent);

        // Handle rate limiting (HTTP 429) with a user-friendly message
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return AuthenticationResult.CreateNotAuthenticated(localizer[LK.Error.RateLimitExceeded]);
        }

        if (!response.IsSuccessStatusCode)
        {
            return AuthenticationResult.CreateNotAuthenticated(localizer[LK.Error.FailedToLogin]);
        }

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthenticationResult>(json, JsonOptionsCache.Options);

        return result ?? AuthenticationResult.CreateNotAuthenticated(localizer[LK.Error.FailedToLogin]);
    }

    /// <inheritdoc/>
    public async Task<ApiResult> LogoutAsync()
    {
        var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
        return await httpClient.PostJsonOrDefaultAsync<object, ApiResult>(AuthenticationRoutes.LogoutFullRoute, null, () => ApiResult.CreateFailure(localizer[LK.Error.Unknown]));
    }

    /// <inheritdoc/>
    public async Task<ApiResultBool> IsAuthenticatedAsync()
    {
        var httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
        return await httpClient.GetJsonOrDefaultAsync(AuthenticationRoutes.StatusFullRoute, () => ApiResultBool.CreateFailure(localizer[LK.Error.FailedToCheckAuthStatus]));
    }

    public Task<bool> IsSessionValidAsync()
    {
        throw new NotImplementedException();
    }
}

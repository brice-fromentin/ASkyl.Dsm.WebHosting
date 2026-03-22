using Askyl.Dsm.WebHosting.Data.Results;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Facade service for authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Attempts to authenticate the user with provided credentials.
    /// Stores DSM SID in server-side session for persistence.
    /// </summary>
    /// <param name="login">The user's login identifier.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="otpCode">Optional one-time password code for two-factor authentication.</param>
    /// <returns>An AuthenticationResult indicating success or failure with message.</returns>
    Task<AuthenticationResult> LoginAsync(string login, string password, string? otpCode);

    /// <summary>
    /// Logs out the current user and clears session state.
    /// </summary>
    /// <returns>An ApiResult indicating logout success or failure.</returns>
    Task<ApiResult> LogoutAsync();

    /// <summary>
    /// Checks if the current session is authenticated via server-side validation.
    /// </summary>
    /// <returns>An ApiResultBool containing a boolean indicating authentication status.</returns>
    Task<ApiResultBool> IsAuthenticatedAsync();
}

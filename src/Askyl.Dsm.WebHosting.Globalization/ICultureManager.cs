using System.Globalization;

namespace Askyl.Dsm.WebHosting.Globalization;

/// <summary>
/// Manages culture for the application. Implemented on the WASM client side
/// to propagate culture to the server via HTTP <c>Accept-Language</c> headers.
/// Culture is resolved once at login from DSM settings and cannot be changed at runtime.
/// </summary>
public interface ICultureManager
{
    /// <summary>
    /// Gets the currently active UI culture.
    /// </summary>
    CultureInfo CurrentUICulture { get; }

    /// <summary>
    /// Gets the currently active culture (same as <see cref="CurrentUICulture"/> for this use case).
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Initializes the culture manager after successful login using the culture from the authentication result.
    /// Falls back to browser language or default culture if the authentication result lacks culture data.
    /// </summary>
    /// <param name="culture">The .NET culture name from the login response (e.g. "en-US", "fr-FR").</param>
    Task InitializeFromLoginAsync(string? culture);
}

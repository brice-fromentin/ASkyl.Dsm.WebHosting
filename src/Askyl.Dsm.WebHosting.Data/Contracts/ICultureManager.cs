using System.Globalization;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Manages culture for the application. Implemented on the WASM client side
/// to propagate culture to the server via HTTP <c>Accept-Language</c> headers.
/// Culture is resolved at construction from DSM system settings, then overridden by user preference at login.
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
    /// Sets the culture after successful login. If the user has no specific preference, keeps the current (system) culture.
    /// Applies user-specific date/time format preferences to the culture's DateTimeFormat when provided.
    /// </summary>
    /// <param name="culture">The .NET culture name from the login response (e.g. "en-US", "fr-FR"), or null if user has no preference.</param>
    /// <param name="dateFormat">The user's date format in .NET format string (e.g. "yyyy/MM/dd"), or null to use culture default.</param>
    /// <param name="timeFormat">The user's time format in .NET format string (e.g. "H:mm"), or null to use culture default.</param>
    void InitializeFromLogin(string? culture, string? dateFormat, string? timeFormat);

    /// <summary>
    /// Resets the culture to the DSM system resolution (system → browser → default). Called on logout.
    /// </summary>
    void ResetToSystem();
}

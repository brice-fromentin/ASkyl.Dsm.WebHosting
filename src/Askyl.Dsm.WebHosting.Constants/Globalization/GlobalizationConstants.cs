using System.Globalization;

namespace Askyl.Dsm.WebHosting.Constants.Globalization;

/// <summary>
/// Constants for culture resolution, localization, and internationalization.
/// </summary>
public static class GlobalizationConstants
{
    #region Default Culture

    /// <summary>
    /// Default culture when no user preference is available.
    /// </summary>
    public const string DefaultCulture = "en-US";

    /// <summary>
    /// Pre-instantiated <see cref="CultureInfo"/> for the default culture.
    /// </summary>
    public static readonly CultureInfo DefaultCultureInfo = new(DefaultCulture);

    #endregion

    #region Text Direction

    /// <summary>
    /// HTML text direction value for left-to-right scripts.
    /// </summary>
    public const string TextDirectionLtr = "ltr";

    /// <summary>
    /// HTML text direction value for right-to-left scripts (e.g. Hebrew, Arabic).
    /// </summary>
    public const string TextDirectionRtl = "rtl";

    #endregion

    #region Environment Variables

    /// <summary>
    /// Environment variable name for supported cultures passed from server to WASM client via <c>Blazor.start()</c>.
    /// </summary>
    public const string SupportedCulturesEnvironmentVariable = "ADWH_SUPPORTED_CULTURES";

    /// <summary>
    /// Environment variable name for DSM system culture passed from server to WASM client via <c>Blazor.start()</c>.
    /// </summary>
    public const string SystemCultureEnvironmentVariable = "ADWH_SYSTEM_CULTURE";

    #endregion

    #region HTTP Headers

    /// <summary>
    /// HTTP header name for client language preference.
    /// </summary>
    public const string AcceptLanguageHeader = "Accept-Language";

    #endregion
}

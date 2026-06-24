using System.Globalization;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Globalization;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Globalization.Extensions;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.JSInterop;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// WASM implementation of <see cref="ICultureManager"/>.
/// Resolves culture at construction from DSM system settings, then overrides with user preference at login.
/// Propagates culture to server via HTTP <c>Accept-Language</c> headers.
/// Culture is not user-selectable via UI — determined by DSM system settings and user login preferences.
/// Supported cultures are discovered from the environment variable injected by the server via <c>Blazor.start()</c>.
/// Browser language is detected from <see cref="CultureInfo.CurrentUICulture"/> auto-set by the WASM runtime.
/// </summary>
/// <param name="jsRuntime">JS runtime for updating HTML lang/dir attributes.</param>
/// <param name="logger">Logger for culture resolution debugging.</param>
public class CultureManager(IJSRuntime jsRuntime, ILogger<ILogCultureManager> logger) : ICultureManager
{
    #region Static Fields

    /// <summary>
    /// Gets the supported cultures discovered from the server-injected environment variable.
    /// </summary>
    internal static CultureInfo[] SupportedCultures { get; } = SafeParseSupportedCultures();

    /// <summary>
    /// Gets the browser's initial culture, captured at class load time before any override.
    /// The WASM runtime sets <see cref="CultureInfo.CurrentUICulture"/> from the Accept-Language header at startup.
    /// </summary>
    internal static CultureInfo BrowserCulture { get; } = SafeGetBrowserCulture();

    /// <summary>
    /// Gets the DSM system culture injected by the server via <c>Blazor.start()</c>.
    /// Returns <c>null</c> when DSM language is set to "def" (browser default).
    /// </summary>
    internal static CultureInfo? SystemCulture { get; } = SafeResolveSystemCultureFromEnv();

    #endregion

    #region Properties

    /// <summary>
    /// Current culture — resolved at construction from DSM system settings, overridden by user preference at login.
    /// </summary>
    public CultureInfo CurrentCulture { get; private set; } = ResolveInitialCulture();

    public CultureInfo CurrentUICulture => CurrentCulture;

    #endregion

    #region ICultureManager Implementation

    /// <inheritdoc/>
    public void InitializeFromLogin(string? culture, string? dateFormat, string? timeFormat)
    {
        // If user has a specific culture, apply it; otherwise fall back to system resolution
        CultureInfo targetCulture;

        if (!String.IsNullOrWhiteSpace(culture))
        {
            try
            {
                var cultureInfo = new CultureInfo(culture);
                var match = FindMatchingCulture(cultureInfo);

                if (match is not null)
                {
                    logger.CultureResolvedFromLogin(match.Name);
                    targetCulture = match;
                }

                else
                {
                    // Unsupported culture — fall back to system resolution (system → browser → default)
                    targetCulture = ResolveSystemCulture();
                    logger.UserCultureUnsupported(culture, targetCulture.Name);
                }
            }

            catch (CultureNotFoundException)
            {
                // Invalid culture name from server — fall back to system resolution
                targetCulture = ResolveSystemCulture();
                logger.UserCultureUnsupported(culture, targetCulture.Name);
            }

            catch (ArgumentException)
            {
                // Invalid culture name format — fall back to system resolution
                targetCulture = ResolveSystemCulture();
                logger.UserCultureUnsupported(culture, targetCulture.Name);
            }
        }

        else
        {
            targetCulture = ResolveSystemCulture();
        }

        // Apply user-specific date/time format overrides
        if (!String.IsNullOrWhiteSpace(dateFormat) || !String.IsNullOrWhiteSpace(timeFormat))
        {
            targetCulture = CloneCultureWithFormats(targetCulture, dateFormat, timeFormat, logger);
        }

        ApplyCulture(targetCulture);
    }

    /// <inheritdoc/>
    public void ResetToSystem()
    {
        var culture = ResolveSystemCulture();
        ApplyCulture(culture);
        logger.CultureResetToSystem(culture.Name);
    }

    #endregion

    #region Construction Helpers

    private static CultureInfo ResolveInitialCulture()
    {
        var culture = ResolveSystemCulture();
        ApplyCultureToThread(culture);
        return culture;
    }

    private static void ApplyCultureToThread(CultureInfo culture)
    {
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
    }

    #endregion

    #region Culture Resolution

    /// <summary>
    /// Resolves culture from DSM system settings, browser language, or default.
    /// Used at construction (login page) and after logout.
    /// </summary>
    private static CultureInfo ResolveSystemCulture()
    {
        // Priority 1: DSM system culture (pre-resolved at class load)
        if (SystemCulture is not null)
        {
            return SystemCulture;
        }

        // Priority 2: browser culture matched against supported cultures
        // Browser may send neutral language (e.g. "fr") — match to supported culture (e.g. "fr-FR")
        var matched = FindMatchingCulture(BrowserCulture);

        return matched ?? BrowserCulture;
    }

    /// <summary>
    /// Finds a matching culture in <see cref="SupportedCultures"/> by exact name or parent language.
    /// </summary>
    private static CultureInfo? FindMatchingCulture(CultureInfo culture)
    {
        // Try exact match first (e.g. "fr-FR")
        var match = SupportedCultures.FirstOrDefault(c => c.Equals(culture));

        if (match is not null)
        {
            return match;
        }

        // Try parent language (e.g. "fr" from "fr-CA")
        return SupportedCultures.FirstOrDefault(c => String.Equals(c.TwoLetterISOLanguageName, culture.TwoLetterISOLanguageName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Resolves the DSM system culture from the environment variable at class load time.
    /// </summary>
    private static CultureInfo? ResolveSystemCultureFromEnv()
    {
        var name = Environment.GetEnvironmentVariable(GlobalizationConstants.SystemCultureEnvironmentVariable);

        if (String.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return FindMatchingCulture(new CultureInfo(name));
    }

    #endregion

    #region Environment Parsing

    private static CultureInfo[] SafeParseSupportedCultures()
    {
        try
        {
            return ParseSupportedCultures();
        }

        catch (JsonException)
        {
            return [GlobalizationConstants.DefaultCultureInfo];
        }

        catch (CultureNotFoundException)
        {
            return [GlobalizationConstants.DefaultCultureInfo];
        }

        catch (ArgumentException)
        {
            return [GlobalizationConstants.DefaultCultureInfo];
        }
    }

    private static CultureInfo[] ParseSupportedCultures()
    {
        var json = Environment.GetEnvironmentVariable(GlobalizationConstants.SupportedCulturesEnvironmentVariable);

        if (String.IsNullOrWhiteSpace(json))
        {
            return [GlobalizationConstants.DefaultCultureInfo];
        }

        var names = JsonSerializer.Deserialize<string[]>(json);

        if (names is null or { Length: 0 })
        {
            return [GlobalizationConstants.DefaultCultureInfo];
        }

        return [.. names.Select(n => new CultureInfo(n))];
    }

    private static CultureInfo SafeGetBrowserCulture()
    {
        try
        {
            return new(CultureInfo.CurrentUICulture.Name);
        }

        catch (CultureNotFoundException)
        {
            return GlobalizationConstants.DefaultCultureInfo;
        }

        catch (ArgumentException)
        {
            return GlobalizationConstants.DefaultCultureInfo;
        }
    }

    private static CultureInfo? SafeResolveSystemCultureFromEnv()
    {
        try
        {
            return ResolveSystemCultureFromEnv();
        }

        catch (CultureNotFoundException)
        {
            return null;
        }

        catch (ArgumentException)
        {
            return null;
        }
    }

    #endregion

    #region Private Helpers

    private void ApplyCulture(CultureInfo culture)
    {
        ApplyCultureToThread(culture);
        CurrentCulture = culture;
        UpdateHtmlLangAndDir(culture);
        logger.CultureApplied(culture.Name);
    }

    private async void UpdateHtmlLangAndDir(CultureInfo culture)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "lang", culture.Name);
            await jsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "dir", culture.GetTextDirection());
        }

        catch (JSException)
        {
            // JS interop may fail during early startup (before DOM is ready) — non-critical, safe to ignore.
        }
    }

    /// <summary>
    /// Creates a clone of the given culture with custom date/time format patterns applied.
    /// </summary>
    private static CultureInfo CloneCultureWithFormats(CultureInfo culture, string? dateFormat, string? timeFormat, ILogger<ILogCultureManager> logger)
    {
        // Clone the culture to avoid modifying the shared static CultureInfo
        var cloned = (CultureInfo)culture.Clone();
        var dtfi = cloned.DateTimeFormat;

        if (!String.IsNullOrWhiteSpace(dateFormat))
        {
            // DSM only exposes a short date format — no long date format is available.
            // Apply the user's preference to both Short and Long patterns so that
            // components using DateTime.ToString("D") (long date) match user expectations.
            try
            {
                dtfi.ShortDatePattern = dateFormat;
                dtfi.LongDatePattern = dateFormat;
            }

            catch (FormatException)
            {
                logger.InvalidDateFormatIgnored(dateFormat);
            }

            catch (NotSupportedException)
            {
                logger.InvalidDateFormatIgnored(dateFormat);
            }
        }

        if (!String.IsNullOrWhiteSpace(timeFormat))
        {
            // Same rationale as date — DSM provides no long time format.
            try
            {
                dtfi.ShortTimePattern = timeFormat;
                dtfi.LongTimePattern = timeFormat;
            }

            catch (FormatException)
            {
                logger.InvalidTimeFormatIgnored(timeFormat);
            }

            catch (NotSupportedException)
            {
                logger.InvalidTimeFormatIgnored(timeFormat);
            }
        }

        return cloned;
    }

    #endregion
}

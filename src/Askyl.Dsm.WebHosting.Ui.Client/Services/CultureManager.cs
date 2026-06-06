using System.Globalization;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Logging;
using Microsoft.JSInterop;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// WASM implementation of <see cref="ICultureManager"/>.
/// Resolves culture once at login from DSM settings, falls back to browser language, then default.
/// Propagates culture to server via HTTP <c>Accept-Language</c> headers.
/// Culture cannot be changed at runtime — it is controlled by DSM user/system preferences.
/// Supported cultures are discovered from the environment variable injected by the server via <c>Blazor.start()</c>.
/// </summary>
/// <param name="jsRuntime">JavaScript runtime for browser language detection.</param>
/// <param name="logger">Logger for culture resolution debugging.</param>
public class CultureManager(IJSRuntime jsRuntime, ILogger<ILogCultureManager> logger) : ICultureManager
{
    /// <summary>
    /// Gets the supported cultures discovered from the server-injected environment variable.
    /// Initialized once when the class is first referenced (at DI registration time).
    /// </summary>
    public static CultureInfo[] SupportedCultures { get; } = ParseSupportedCultures();

    public CultureInfo CurrentCulture { get; private set; } = new(GlobalizationServiceCollectionExtensions.DefaultCulture);

    public CultureInfo CurrentUICulture => CurrentCulture;

    private static CultureInfo[] ParseSupportedCultures()
    {
        var json = Environment.GetEnvironmentVariable(ApplicationConstants.SupportedCulturesEnvironmentVariable);

        if (String.IsNullOrWhiteSpace(json))
        {
            return [new CultureInfo(GlobalizationServiceCollectionExtensions.DefaultCulture)];
        }

        var names = JsonSerializer.Deserialize<string[]>(json);

        if (names is null or { Length: 0 })
        {
            return [new CultureInfo(GlobalizationServiceCollectionExtensions.DefaultCulture)];
        }

        return [.. names.Select(n => new CultureInfo(n))];
    }

    public Task InitializeFromLoginAsync(string? culture)
    {
        string? resolved = null;

        // Priority 1: culture from login response (server resolved user vs system preference)
        if (!String.IsNullOrWhiteSpace(culture) && SupportedCultures.Any(c => String.Equals(c.Name, culture, StringComparison.OrdinalIgnoreCase)))
        {
            resolved = culture;
            logger.CultureResolvedFromLogin(culture);
        }

        // Priority 2: browser navigator.language
        if (resolved is null)
        {
            resolved = DetectBrowserLanguage();

            if (resolved is not null)
            {
                logger.CultureResolvedFromBrowser(resolved);
            }
        }

        // Priority 3: default
        resolved ??= GlobalizationServiceCollectionExtensions.DefaultCulture;
        logger.CultureResolvedToDefault(resolved);

        return SetCultureInternalAsync(resolved);
    }

    private Task SetCultureInternalAsync(string cultureName)
    {
        var culture = SupportedCultures.FirstOrDefault(c => String.Equals(c.Name, cultureName, StringComparison.OrdinalIgnoreCase));

        // Fallback to default for unsupported cultures
        culture ??= new CultureInfo(GlobalizationServiceCollectionExtensions.DefaultCulture);

        // Apply culture to current thread
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        CurrentCulture = culture;

        logger.CultureApplied(culture.Name);

        return Task.CompletedTask;
    }

    private string? DetectBrowserLanguage()
    {
        try
        {
            var browserLang = jsRuntime.InvokeAsync<string>(ApplicationConstants.JsInteropNavigatorLanguageGet).Result;

            if (String.IsNullOrWhiteSpace(browserLang))
            {
                return null;
            }

            // Try exact match first (e.g. "fr-FR")
            var match = SupportedCultures.FirstOrDefault(c => String.Equals(c.Name, browserLang, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match.Name;
            }

            // Try parent culture (e.g. "fr" from "fr-CA")
            var parentName = browserLang.Split('-')[0];
            match = SupportedCultures.FirstOrDefault(c => String.Equals(c.Name, parentName, StringComparison.OrdinalIgnoreCase) || String.Equals(c.Parent.Name, parentName, StringComparison.OrdinalIgnoreCase));
            return match?.Name;
        }
        catch (Exception ex)
        {
            // JS interop failure — return null to let default culture win
            logger.BrowserLanguageDetectionFailed(ex);
            return null;
        }
    }
}

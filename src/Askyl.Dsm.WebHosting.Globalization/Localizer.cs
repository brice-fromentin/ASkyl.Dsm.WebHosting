using System.Globalization;
using System.Resources;

namespace Askyl.Dsm.WebHosting.Globalization;

/// <summary>
/// Abstraction over resource lookup that hides the Microsoft implementation detail.
/// Use <c>T[key]</c> for simple strings, <c>T[key, arg]</c> or <c>T[key, arg1, arg2]</c> for formatted strings.
/// </summary>
public interface ILocalizer
{
    /// <summary>
    /// Gets a localized string by key, optionally formatting with the provided arguments.
    /// </summary>
    string this[string name, params object[] arguments] { get; }
}

/// <summary>
/// Wraps <see cref="ResourceManager"/> behind <see cref="ILocalizer"/>.
/// Uses <see cref="ResourceManager"/> directly (not <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/>)
/// because the latter caches culture at construction time in WASM — culture changes after login are ignored.
/// <see cref="ResourceManager.GetString(string, CultureInfo)"/> always reads <see cref="CultureInfo.CurrentUICulture"/>
/// at call time, so culture switches work correctly without re-rendering.
/// </summary>
public sealed class Localizer : ILocalizer
{
    static Localizer()
    {
        SharedResource = new ResourceManager(
            "Askyl.Dsm.WebHosting.Globalization.Resources.SharedResource",
            typeof(Resources.SharedResource).Assembly);
    }

    /// <summary>Shared <see cref="ResourceManager"/> for the SharedResource resource file.</summary>
    public static ResourceManager SharedResource { get; }

    /// <inheritdoc />
    public string this[string name, params object[] arguments]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture;
            var value = SharedResource.GetString(name, culture) ?? $"[{name}]";

            return arguments.Length == 0 ? value : String.Format(culture, value, arguments);
        }
    }
}

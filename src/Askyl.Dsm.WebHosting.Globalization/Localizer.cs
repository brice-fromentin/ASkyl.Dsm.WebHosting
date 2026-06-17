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
    LocalizedText this[string name, params object[] arguments] { get; }
}

/// <summary>
/// Wraps <see cref="ResourceManager"/> behind <see cref="ILocalizer"/>.
/// Uses <see cref="ResourceManager"/> directly (not <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/>)
/// because the latter caches culture at construction time in WASM — culture changes after login are ignored.
/// <see cref="ResourceManager.GetString(string, CultureInfo)"/> always reads <see cref="CultureInfo.CurrentUICulture"/>
/// at call time, so culture switches work correctly without re-rendering.
/// </summary>
/// <param name="resourceManager">Pre-configured resource manager for SharedResource.</param>
public sealed class Localizer(ResourceManager resourceManager) : ILocalizer
{
    /// <inheritdoc />
    public LocalizedText this[string name, params object[] arguments]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture;
            var value = resourceManager.GetString(name, culture) ?? $"[{name}]";
            return arguments.Length == 0
                ? new LocalizedText(name, value)
                : new LocalizedText(name, String.Format(culture, value, arguments));
        }
    }
}

/// <summary>
/// Holds the shared <see cref="ResourceManager"/> for the SharedResource resource file.
/// </summary>
public static class ResourceManagerCache
{
    /// <summary>
    /// Gets the resource manager for the SharedResource file.
    /// </summary>
    public static ResourceManager SharedResource { get; } = new(
        "Askyl.Dsm.WebHosting.Globalization.Resources.SharedResource",
        typeof(Resources.SharedResource).Assembly);
}

/// <summary>
/// Represents a localized string returned by <see cref="ILocalizer"/>.
/// </summary>
/// <param name="name">The resource key.</param>
/// <param name="value">The localized value.</param>
public sealed class LocalizedText(string name, string value)
{
    /// <summary>The resource key.</summary>
    public string Name { get; } = name;

    /// <summary>The localized value (falls back to key name if not found).</summary>
    public string Value { get; } = value;

    /// <summary>Implicit conversion to string — returns empty string if text is null (defensive for test mocks).</summary>
    public static implicit operator string(LocalizedText? text) => text?.Value ?? String.Empty;
}

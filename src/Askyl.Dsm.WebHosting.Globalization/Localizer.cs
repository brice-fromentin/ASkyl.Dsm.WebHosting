using Microsoft.Extensions.Localization;

namespace Askyl.Dsm.WebHosting.Globalization;

/// <summary>
/// Abstraction over <see cref="IStringLocalizer{T}"/> that hides the Microsoft implementation detail.
/// Use <c>T[key]</c> for simple strings, <c>T[key, arg]</c> or <c>T[key, arg1, arg2]</c> for formatted strings.
/// </summary>
public interface ILocalizer
{
    /// <summary>
    /// Gets a localized string by key, optionally formatting with the provided arguments.
    /// </summary>
    LocalizedString this[string name, params object[] arguments] { get; }
}

/// <summary>
/// Wraps <see cref="IStringLocalizer{SharedResource}"/> behind <see cref="ILocalizer"/>,
/// hiding the Microsoft implementation from consumer projects.
/// </summary>
public sealed class Localizer(IStringLocalizer<Resources.SharedResource> localizer) : ILocalizer
{
    /// <inheritdoc />
    public LocalizedString this[string name, params object[] arguments] => arguments.Length == 0 ? localizer[name] : localizer[name, arguments];
}

using System.Globalization;
using FluentValidation;

namespace Askyl.Dsm.WebHosting.Globalization.Validators;

/// <summary>
/// Extension methods for attaching deferred (runtime) localization to FluentValidation rules.
/// </summary>
public static class DeferredMessageExtensions
{
    extension<T, TValue>(IRuleBuilderOptions<T, TValue> ruleOptions)
    {
        /// <summary>
        /// Sets a localized error message that is resolved at validation time (not constructor time).
        /// Use this instead of <c>.WithMessage(localizer[someKey])</c> to support runtime culture switches.
        /// </summary>
        public IRuleBuilderOptions<T, TValue> WithLocalizedMessage(string resourceKey)
        {
            // The Func overload defers resolution to validation time.
            // At that point, CultureInfo.CurrentUICulture reflects the active culture.
            return ruleOptions.WithMessage(_ => ResolveResource(resourceKey));
        }
    }

    static string ResolveResource(string key)
    {
        var culture = CultureInfo.CurrentUICulture;
        return Localizer.SharedResource.GetString(key, culture) ?? $"[{key}]";
    }
}

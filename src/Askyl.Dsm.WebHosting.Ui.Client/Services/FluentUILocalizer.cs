using System.Globalization;
using Askyl.Dsm.WebHosting.Constants.Globalization;
using Askyl.Dsm.WebHosting.Globalization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Serves FluentUI's built-in strings — message box titles and buttons, grid labels — from the application's own
/// resources, in the culture the rest of the UI uses. FluentUI 5 ships English only.
/// </summary>
/// <remarks>
/// A string is looked up as <see cref="GlobalizationConstants.FluentUIResourcePrefix"/> plus FluentUI's key, and one the
/// application does not translate keeps FluentUI's English.
/// </remarks>
public sealed class FluentUILocalizer : IFluentLocalizer
{
    /// <inheritdoc />
    public string this[string key, params object[] arguments]
    {
        get
        {
            var culture = CultureInfo.CurrentUICulture;
            var value = Localizer.SharedResource.GetString(GlobalizationConstants.FluentUIResourcePrefix + key, culture);

            if (value is null)
            {
                return IFluentLocalizer.GetDefault(key, arguments);
            }

            return arguments.Length == 0 ? value : String.Format(culture, value, arguments);
        }
    }
}

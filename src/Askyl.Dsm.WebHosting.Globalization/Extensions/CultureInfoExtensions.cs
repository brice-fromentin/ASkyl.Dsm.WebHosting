using System.Globalization;
using Askyl.Dsm.WebHosting.Constants.Globalization;

namespace Askyl.Dsm.WebHosting.Globalization.Extensions;

/// <summary>
/// Extension methods for <see cref="CultureInfo"/>.
/// </summary>
public static class CultureInfoExtensions
{
    extension(CultureInfo culture)
    {
        /// <summary>
        /// Gets the HTML text direction for the culture (<c>"ltr"</c> or <c>"rtl"</c>).
        /// </summary>
        public string GetTextDirection()
        {
            return culture.TextInfo.IsRightToLeft ? GlobalizationConstants.TextDirectionRtl : GlobalizationConstants.TextDirectionLtr;
        }
    }
}

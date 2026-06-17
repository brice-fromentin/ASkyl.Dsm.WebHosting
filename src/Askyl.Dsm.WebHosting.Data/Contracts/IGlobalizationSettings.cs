using System.Globalization;

namespace Askyl.Dsm.WebHosting.Data.Contracts;

/// <summary>
/// Provides globalization settings discovered at server startup.
/// </summary>
public interface IGlobalizationSettings
{
    /// <summary>
    /// Gets the supported cultures discovered from embedded satellite resources.
    /// </summary>
    CultureInfo[] SupportedCultures { get; }

    /// <summary>
    /// Gets the supported culture names as a JSON array string (for WASM environment injection).
    /// </summary>
    string SupportedCultureNamesJson { get; }

    /// <summary>
    /// Gets or sets the DSM system culture (converted from DSM language code).
    /// Set once at startup by <see cref="Extensions.GlobalizationExtensions.ApplyDsmSystemCulture"/>.
    /// </summary>
    string? SystemCulture { get; set; }
}

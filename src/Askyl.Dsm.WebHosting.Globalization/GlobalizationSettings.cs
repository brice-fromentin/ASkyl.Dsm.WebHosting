using System.Globalization;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Globalization.Resources;

namespace Askyl.Dsm.WebHosting.Globalization;

/// <summary>
/// Static settings for globalization — populated at server startup and injected to WASM.
/// </summary>
public static class GlobalizationSettings
{
    /// <summary>
    /// Gets the discovered supported cultures from embedded satellite resources.
    /// </summary>
    public static CultureInfo[] SupportedCultures { get; } = DiscoverSupportedCultures();

    /// <summary>
    /// Gets the discovered supported culture names as a JSON array (for App.razor injection).
    /// </summary>
    public static string SupportedCultureNamesJson { get; } = JsonSerializer.Serialize(SupportedCultures.Select(c => c.Name).ToArray());

    /// <summary>
    /// Gets or sets the DSM system culture (converted from DSM language code).
    /// Populated at server startup from /etc/synoinfo.conf.
    /// </summary>
    public static string? SystemCulture { get; set; }

    private static CultureInfo[] DiscoverSupportedCultures()
    {
        var assembly = typeof(SharedResource).Assembly;
        var assemblyDirectory = Path.GetDirectoryName(assembly.Location)!;
        var satelliteAssemblyName = $"{assembly.GetName().Name}.resources.dll";

        var cultureNames = Directory.GetDirectories(assemblyDirectory)
                                    .Select(dir => Path.GetFileName(dir)!)
                                    .Where(name => !name.StartsWith('.')
                                                   && CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => String.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                                                   && File.Exists(Path.Combine(assemblyDirectory, name, satelliteAssemblyName)))
                                    .Concat([GlobalizationServiceCollectionExtensions.DefaultCulture])
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        return [.. cultureNames.Select(name => new CultureInfo(name))];
    }
}

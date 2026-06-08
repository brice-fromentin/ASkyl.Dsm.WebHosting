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
    /// Written once during startup — no synchronization needed.
    /// </summary>
    public static string? SystemCulture { get; set; }

    private static CultureInfo[] DiscoverSupportedCultures()
    {
        var assembly = typeof(SharedResource).Assembly;
        var assemblyPath = assembly.Location;
        var assemblyName = assembly.GetName().Name;

        if (String.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new InvalidOperationException($"Cannot determine assembly location for '{assemblyName}'.");
        }

        var assemblyDirectory = Path.GetDirectoryName(assemblyPath)!;
        var satelliteAssemblyName = $"{assemblyName}.resources.dll";

        var validCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                                       .Select(c => c.Name)
                                       .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var cultureNames = Directory.GetDirectories(assemblyDirectory)
                                    .Select(dir => Path.GetFileName(dir)!)
                                    .Where(name => !name.StartsWith('.')
                                                   && validCultures.Contains(name)
                                                   && File.Exists(Path.Combine(assemblyDirectory, name, satelliteAssemblyName)))
                                    .Concat([GlobalizationServiceCollectionExtensions.DefaultCulture])
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        return [.. cultureNames.Select(name => new CultureInfo(name))];
    }
}

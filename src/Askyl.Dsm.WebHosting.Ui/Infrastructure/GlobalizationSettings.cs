using System.Globalization;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Constants.Globalization;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Globalization.Resources;
using Askyl.Dsm.WebHosting.Logging;

namespace Askyl.Dsm.WebHosting.Ui.Infrastructure;

/// <summary>
/// Server-only globalization settings — discovers supported cultures from satellite resources at construction time.
/// </summary>
public class GlobalizationSettings(ILogger<ILogGlobalizationSettings> logger) : IGlobalizationSettings
{
    private readonly (CultureInfo[] Cultures, string Json) _data = DiscoverSupportedCulturesWithJson(logger);

    public CultureInfo[] SupportedCultures => _data.Cultures;

    public string SupportedCultureNamesJson => _data.Json;

    public string? SystemCulture { get; set; }

    private static (CultureInfo[] Cultures, string Json) DiscoverSupportedCulturesWithJson(ILogger<ILogGlobalizationSettings> log)
    {
        var cultures = DiscoverSupportedCultures(log);
        var json = JsonSerializer.Serialize(cultures.Select(c => c.Name).ToArray());
        return (cultures, json);
    }

    private static CultureInfo[] DiscoverSupportedCultures(ILogger<ILogGlobalizationSettings> log)
    {
        var assembly = typeof(SharedResource).Assembly;
        var assemblyName = assembly.GetName().Name!;

        log.DiscoveringCultures(assemblyName);

        var assemblyPath = assembly.Location;

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
                                    .Concat([GlobalizationConstants.DefaultCulture])
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);

        var cultures = new List<CultureInfo>();

        foreach (var name in cultureNames)
        {
            try
            {
                cultures.Add(new(name));
            }
            catch (CultureNotFoundException)
            {
                log.CultureSkippedNotSupported(name);
            }
        }

        log.CulturesDiscovered(cultures.Count);

        return [.. cultures];
    }
}

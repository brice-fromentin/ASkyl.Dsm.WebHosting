using System.Globalization;
using System.Text.Json;
using Askyl.Dsm.WebHosting.Globalization;
using Askyl.Dsm.WebHosting.Globalization.Resources;

namespace Askyl.Dsm.WebHosting.Ui;

/// <summary>
/// Server-side globalization extensions for request localization.
/// Discovers supported cultures from embedded satellite resources at startup.
/// </summary>
public static class GlobalizationExtensions
{
    /// <summary>
    /// Gets the discovered supported cultures (server-side only).
    /// </summary>
    public static CultureInfo[] SupportedCultures { get; } = DiscoverSupportedCultures();

    /// <summary>
    /// Gets the discovered supported culture names as a JSON array string (for App.razor injection).
    /// </summary>
    public static string SupportedCultureNamesJson { get; } = JsonSerializer.Serialize(SupportedCultures.Select(c => c.Name).ToArray());

    private static CultureInfo[] DiscoverSupportedCultures()
    {
        var assembly = typeof(SharedResource).Assembly;
        var assemblyDirectory = Path.GetDirectoryName(assembly.Location)!;
        var satelliteAssemblyName = $"{assembly.GetName().Name}.resources.dll";

        // Discover cultures from satellite assembly directories (fr-FR/, de-DE/, etc.)
        // The SDK places satellite resources in culture-named subdirectories
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

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures request localization for supported cultures discovered from resource files.
        /// </summary>
        public IServiceCollection ConfigureGlobalizationRequestLocalization()
        {
            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new(GlobalizationServiceCollectionExtensions.DefaultCulture);
                options.SupportedCultures = SupportedCultures;
                options.SupportedUICultures = SupportedCultures;
            });

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Adds the request localization middleware to the pipeline.
        /// </summary>
        public IApplicationBuilder UseGlobalizationRequestLocalization()
        {
            return app.UseRequestLocalization();
        }
    }
}

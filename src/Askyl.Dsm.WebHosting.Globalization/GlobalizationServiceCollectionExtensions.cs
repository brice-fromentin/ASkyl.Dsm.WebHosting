using Askyl.Dsm.WebHosting.Globalization.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace Askyl.Dsm.WebHosting.Globalization;

/// <summary>
/// Extension methods for registering globalization services.
/// </summary>
public static class GlobalizationServiceCollectionExtensions
{
    /// <summary>
    /// Default culture when no user preference is available.
    /// </summary>
    public const string DefaultCulture = "en-US";

    /// <summary>
    /// Adds localization services for the SharedResource resource file.
    /// Call this in both server and client <c>Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddGlobalization(this IServiceCollection services)
    {
        // AddLocalization registers IStringLocalizer<> factory.
        // ResourcesPath is intentionally omitted — resources are discovered by the
        // full type name (Askyl.Dsm.WebHosting.Globalization.Resources.SharedResource),
        // which matches the embedded resource name from the .resx files in Resources/ folder.
        services.AddLocalization();

        // Wrap IStringLocalizer<SharedResource> behind ILocalizer to hide Microsoft internals.
        // Singleton is safe — LocalizedString is immutable and culture is resolved at call time.
        services.AddSingleton<ILocalizer>(sp => new Localizer(sp.GetRequiredService<IStringLocalizer<SharedResource>>()));

        return services;
    }
}

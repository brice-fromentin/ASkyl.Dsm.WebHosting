using Microsoft.Extensions.DependencyInjection;
using Askyl.Dsm.WebHosting.Globalization.Resources;
using Microsoft.Extensions.Localization;

namespace Askyl.Dsm.WebHosting.Globalization;

/// <summary>
/// Extension methods for registering globalization services.
/// </summary>
public static class GlobalizationServiceCollectionExtensions
{
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

        return services;
    }
}

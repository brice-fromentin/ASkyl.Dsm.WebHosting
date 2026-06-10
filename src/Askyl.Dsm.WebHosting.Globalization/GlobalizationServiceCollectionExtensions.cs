using Microsoft.Extensions.DependencyInjection;

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
    /// Adds the <see cref="ILocalizer"/> service for accessing SharedResource translations.
    /// Call this in both server and client <c>Program.cs</c>.
    /// </summary>
    public static IServiceCollection AddGlobalization(this IServiceCollection services)
    {
        // ResourceManager-based localizer — reads CurrentUICulture at call time,
        // so culture changes after login are picked up without re-rendering.
        services.AddSingleton<ILocalizer>(new Localizer(ResourceManagerCache.SharedResource));

        return services;
    }
}

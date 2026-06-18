using Microsoft.Extensions.DependencyInjection;

namespace Askyl.Dsm.WebHosting.Globalization.Extensions;

/// <summary>
/// Extension methods for registering globalization services.
/// </summary>
public static class GlobalizationServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the <see cref="ILocalizer"/> service for accessing SharedResource translations.
        /// Call this in both server and client <c>Program.cs</c>.
        /// </summary>
        public IServiceCollection AddGlobalization()
        {
            // ResourceManager-based localizer — reads CurrentUICulture at call time,
            // so culture changes after login are picked up without re-rendering.
            services.AddSingleton<ILocalizer>(new Localizer(ResourceManagerCache.SharedResource));

            return services;
        }
    }
}

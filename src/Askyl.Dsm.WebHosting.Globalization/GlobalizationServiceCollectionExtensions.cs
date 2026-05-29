namespace Microsoft.Extensions.DependencyInjection;

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
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        return services;
    }
}

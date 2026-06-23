using Askyl.Dsm.WebHosting.Constants.Globalization;
using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Logging;
using Askyl.Dsm.WebHosting.Tools.Converters;
using Askyl.Dsm.WebHosting.Tools.Infrastructure;

namespace Askyl.Dsm.WebHosting.Ui.Extensions;

/// <summary>
/// ASP.NET Core extensions for globalization request localization.
/// </summary>
public static class GlobalizationExtensions
{
    /// <summary>
    /// Fetches the DSM system language and applies it to <see cref="IGlobalizationSettings.SystemCulture"/>.
    /// Call once after <see cref="WebApplication.Build"/> before any middleware.
    /// </summary>
    public static void ApplyDsmSystemCulture(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        var dsmSettings = serviceProvider.GetRequiredService<IDsmSettingsService>();
        var settings = serviceProvider.GetRequiredService<IGlobalizationSettings>();
        var systemCulture = DsmLanguageToCultureConverter.Convert(dsmSettings.Language);
        settings.SystemCulture = systemCulture;

        if (!String.IsNullOrWhiteSpace(systemCulture))
        {
            var logger = serviceProvider.GetRequiredService<ILogger<ILogGlobalizationSettings>>();
            logger.SystemCultureSet(systemCulture);
        }
    }

    /// <summary>
    /// Configures request localization options from <see cref="IGlobalizationSettings"/>
    /// and adds the request localization middleware to the pipeline.
    /// Must be called after <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton"/> registration of <see cref="IGlobalizationSettings"/>.
    /// </summary>
    public static IApplicationBuilder UseGlobalizationRequestLocalization(this IApplicationBuilder app)
    {
        var settings = app.ApplicationServices.GetRequiredService<IGlobalizationSettings>();

        var options = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new(GlobalizationConstants.DefaultCultureInfo),
            SupportedCultures = settings.SupportedCultures,
            SupportedUICultures = settings.SupportedCultures
        };

        return app.UseRequestLocalization(options);
    }
}

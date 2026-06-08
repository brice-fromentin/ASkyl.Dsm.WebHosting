using Askyl.Dsm.WebHosting.Globalization;

namespace Askyl.Dsm.WebHosting.Ui.Extensions;

/// <summary>
/// ASP.NET Core extensions for globalization request localization.
/// </summary>
public static class GlobalizationExtensions
{
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
                options.SupportedCultures = GlobalizationSettings.SupportedCultures;
                options.SupportedUICultures = GlobalizationSettings.SupportedCultures;
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

using Askyl.Dsm.WebHosting.Constants;
using Askyl.Dsm.WebHosting.Data.API.Definitions;
using Askyl.Dsm.WebHosting.Tools.Network;
using Microsoft.Extensions.DependencyInjection;

namespace Askyl.Dsm.WebHosting.Tools;

public static class DsmToolsExtensions
{
    public static IServiceCollection AddDsmApiClient(this IServiceCollection services)
    {
        services.AddHttpClient(DsmDefaults.HttpClientName).ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
        });

        services.AddScoped<DsmApiClient>();
        services.AddScoped((provider) => provider.GetRequiredService<DsmApiClient>().ApiInformations);

        return services;
    }
}

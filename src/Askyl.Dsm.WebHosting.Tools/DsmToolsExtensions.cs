using Microsoft.Extensions.DependencyInjection;

using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Tools.Network;

namespace Askyl.Dsm.WebHosting.Tools;

public static class DsmToolsExtensions
{
    public static IServiceCollection AddDsmApiClient(this IServiceCollection services)
    {
        services.AddHttpClient(ApplicationConstants.HttpClientName);

        services.AddSingleton<DsmApiClient>();
        services.AddSingleton((provider) => provider.GetRequiredService<DsmApiClient>().ApiInformations);

        return services;
    }
}

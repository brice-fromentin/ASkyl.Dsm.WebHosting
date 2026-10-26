using System.Net;
using System.Security.Authentication;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Tests.Ui.Services;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Ui.Components;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Askyl.Dsm.WebHosting.Tests.Ui;

/// <summary>
/// The channel that carries the DSM password, exercised through the real service registrations: the named
/// client must hold full validation for names and relax it only for a loopback literal root owns.
/// </summary>
public class DsmHttpClientWiringTests(ApplicationHostFactory factory) : IClassFixture<ApplicationHostFactory>
{
    sealed class FakeRootListenerProbe : IRootListenerProbe
    {
        public bool IsRootListening(IPAddress loopbackAddress, int port) => true;
    }

    WebApplicationFactory<App> WithRootListener() => factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
    {
        services.RemoveAll<IRootListenerProbe>();
        services.AddSingleton<IRootListenerProbe, FakeRootListenerProbe>();
    }));

    static async Task<Exception> GetFailureAsync(WebApplicationFactory<App> host, string address, int port)
    {
        var client = host.Services.GetRequiredService<IHttpClientFactory>().CreateClient(ApplicationConstants.DsmHttpClientName);

        return await Assert.ThrowsAnyAsync<Exception>(() => client.GetAsync($"https://{address}:{port}/"));
    }

    static bool HasInner<T>(Exception? exception) where T : Exception
        => exception is not null && (exception is T || HasInner<T>(exception.InnerException));

    [Fact]
    public async Task DsmClient_RejectsTheSelfSignedCertificate_ForAName()
    {
        using var host = WithRootListener();
        await using var server = LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var failure = await GetFailureAsync(host, NetworkConstants.Localhost, server.Port);

        Assert.True(HasInner<AuthenticationException>(failure), failure.ToString());
    }

    [Fact]
    public async Task DsmClient_GetsPastTls_ForARootOwnedLoopbackLiteral()
    {
        using var host = WithRootListener();
        await using var server = LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var failure = await GetFailureAsync(host, IPAddress.Loopback.ToString(), server.Port);

        // The test server closes after the handshake, so something fails later, but not the certificate
        Assert.False(HasInner<AuthenticationException>(failure), failure.ToString());
    }
}

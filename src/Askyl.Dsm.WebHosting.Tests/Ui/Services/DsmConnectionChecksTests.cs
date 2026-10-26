using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Ui.Services;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DsmConnectionChecksTests
{
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    static readonly string LoopbackAddress = IPAddress.Loopback.ToString();

    static DsmCertificatePolicy Policy(bool rootListens) => new(Mock.Of<IRootListenerProbe>(probe => probe.IsRootListening(It.IsAny<IPAddress>(), It.IsAny<int>()) == rootListens));

    static LocalTlsServer SelfSignedServer() => LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

    #region Port

    [Fact]
    public async Task ConnectAsync_AClosedPort_ReportsPortRefused()
    {
        // Reserve a port, then release it: nothing listens there any more
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        Assert.Equal(DsmCheckFailure.PortRefused, await DsmConnectionChecks.ConnectAsync(LoopbackAddress, port, Timeout));
    }

    #endregion

    #region Trust

    [Fact]
    public async Task InspectCertificateAsync_ASelfSignedCertificate_IsNeverAcceptedForAName()
    {
        // The guarantee that the check observes validation instead of bypassing it: localhost is a name, never relaxed
        await using var server = SelfSignedServer();

        var (failure, errors) = await DsmConnectionChecks.InspectCertificateAsync(NetworkConstants.Localhost, server.Port, Policy(rootListens: true), Timeout);

        Assert.Equal(DsmCheckFailure.NotTrusted, failure);
        Assert.True(errors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public async Task InspectCertificateAsync_ARootOwnedLoopbackLiteral_IsTrusted()
    {
        await using var server = SelfSignedServer();

        var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(LoopbackAddress, server.Port, Policy(rootListens: true), Timeout);

        Assert.Equal(DsmCheckFailure.None, failure);
    }

    [Fact]
    public async Task InspectCertificateAsync_ALoopbackLiteralNobodyRootOwns_IsNotTrusted()
    {
        await using var server = SelfSignedServer();

        var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(LoopbackAddress, server.Port, Policy(rootListens: false), Timeout);

        Assert.Equal(DsmCheckFailure.NotTrusted, failure);
    }

    [Fact]
    public async Task InspectCertificateAsync_AServerThatNeverSpeaksTls_IsBoundedByTheTimeout()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var stopwatch = Stopwatch.StartNew();

            var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(LoopbackAddress, port, Policy(rootListens: true), TimeSpan.FromMilliseconds(300));

            Assert.Equal(DsmCheckFailure.HandshakeFailed, failure);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
        }
        finally
        {
            listener.Stop();
        }
    }

    #endregion
}

using System.Diagnostics;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Network;
using Askyl.Dsm.WebHosting.Ui.Services;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DsmConnectionChecksTests
{
    /// <summary>
    /// Reserved by RFC 6761: never resolves, so the test needs no outside network.
    /// </summary>
    const string UnresolvableHost = "dsm.invalid";

    /// <summary>
    /// Not a loopback IP literal, so it gets full validation; the test certificate covers localhost only.
    /// </summary>
    const string NonLoopbackName = "dsm.test";

    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    static readonly string LoopbackAddress = IPAddress.Loopback.ToString();

    static DsmCertificatePolicy Policy(bool rootListens) => new(Mock.Of<IRootListenerProbe>(probe => probe.IsRootListening(It.IsAny<IPAddress>(), It.IsAny<int>()) == rootListens));

    #region Name Resolution

    [Fact]
    public async Task ResolveAsync_AnUnresolvableName_ReportsNameNotFound()
        => Assert.Equal(DsmCheckFailure.NameNotFound, (await DsmConnectionChecks.ResolveAsync(UnresolvableHost, Timeout)).Failure);

    [Fact]
    public async Task ResolveAsync_AnIpLiteral_Passes()
        => Assert.Equal(DsmCheckFailure.None, (await DsmConnectionChecks.ResolveAsync(LoopbackAddress, Timeout)).Failure);

    #endregion

    #region Port

    [Fact]
    public async Task ConnectAsync_AClosedPort_ReportsPortRefused()
    {
        // Reserve a port, then release it: nothing listens there any more
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        Assert.Equal(DsmCheckFailure.PortRefused, await DsmConnectionChecks.ConnectAsync([IPAddress.Loopback], port, Timeout));
    }

    #endregion

    #region Certificate

    [Fact]
    public async Task InspectCertificateAsync_ASelfSignedCertificate_IsNeverAccepted()
    {
        // The guarantee that the check observes validation instead of bypassing it
        await using var server = LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var (failure, certificate) = await DsmConnectionChecks.InspectCertificateAsync(NetworkConstants.Localhost, [IPAddress.Loopback], server.Port, Policy(rootListens: true), Timeout);

        Assert.Equal(DsmCheckFailure.CertificateUntrusted, failure);
        Assert.NotNull(certificate);
        Assert.Contains(NetworkConstants.Localhost, certificate.Names);
    }

    [Fact]
    public async Task InspectCertificateAsync_ANameTheCertificateDoesNotCover_ReportsNameMismatch()
    {
        await using var server = LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(NonLoopbackName, [IPAddress.Loopback], server.Port, Policy(rootListens: true), Timeout);

        Assert.Equal(DsmCheckFailure.CertificateNameMismatch, failure);
    }

    [Fact]
    public async Task InspectCertificateAsync_ALoopbackIpLiteral_AcceptsTheSelfSignedCertificate()
    {
        await using var server = LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var (failure, certificate) = await DsmConnectionChecks.InspectCertificateAsync(LoopbackAddress, [IPAddress.Loopback], server.Port, Policy(rootListens: true), Timeout);

        Assert.Equal(DsmCheckFailure.None, failure);
        Assert.NotNull(certificate);
    }

    [Fact]
    public async Task InspectCertificateAsync_ALoopbackLiteralNobodyRootOwns_IsNotAccepted()
    {
        await using var server = LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(LoopbackAddress, [IPAddress.Loopback], server.Port, Policy(rootListens: false), Timeout);

        Assert.Equal(DsmCheckFailure.CertificateNameMismatch, failure);
    }

    [Fact]
    public async Task InspectCertificateAsync_AnExpiredCertificate_ReportsExpired()
    {
        await using var server = LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1)));

        var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(NetworkConstants.Localhost, [IPAddress.Loopback], server.Port, Policy(rootListens: true), Timeout);

        Assert.Equal(DsmCheckFailure.CertificateExpired, failure);
    }

    [Fact]
    public async Task InspectCertificateAsync_AnIpCertificate_ShowsNoIpAddress()
    {
        await using var server = LocalTlsServer.Start(TestCertificates.CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), "CN=" + LoopbackAddress, IPAddress.Loopback));

        var (_, certificate) = await DsmConnectionChecks.InspectCertificateAsync(NetworkConstants.Localhost, [IPAddress.Loopback], server.Port, Policy(rootListens: true), Timeout);

        Assert.NotNull(certificate);
        Assert.DoesNotContain(certificate.Names, name => IPAddress.TryParse(name, out _));
        Assert.Equal(String.Empty, certificate.Issuer);
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

            var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(NetworkConstants.Localhost, [IPAddress.Loopback], port, Policy(rootListens: true), TimeSpan.FromMilliseconds(300));

            Assert.Equal(DsmCheckFailure.HandshakeFailed, failure);
            Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(2));
        }
        finally
        {
            listener.Stop();
        }
    }

    [Theory]
    [InlineData(SslPolicyErrors.None)]
    [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable)]
    public void ClassifyCertificateErrors_WithoutACertificateToBlame_ReportsHandshakeFailed(SslPolicyErrors errors)
        => Assert.Equal(DsmCheckFailure.HandshakeFailed, DsmConnectionChecks.ClassifyCertificateErrors(errors, X509ChainStatusFlags.NoError));

    #endregion
}

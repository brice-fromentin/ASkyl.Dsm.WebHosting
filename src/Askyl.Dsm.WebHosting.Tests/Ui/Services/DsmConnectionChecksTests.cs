using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Askyl.Dsm.WebHosting.Constants.Network;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;
using Askyl.Dsm.WebHosting.Ui.Services;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

public class DsmConnectionChecksTests
{
    /// <summary>
    /// Reserved by RFC 6761: never resolves, so the test needs no outside network.
    /// </summary>
    const string UnresolvableHost = "dsm.invalid";

    const string CertificateSubject = "CN=localhost";

    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    static readonly string LoopbackAddress = IPAddress.Loopback.ToString();

    #region Name Resolution

    [Fact]
    public async Task ResolveAsync_AnUnresolvableName_ReportsNameNotFound()
        => Assert.Equal(DsmCheckFailure.NameNotFound, await DsmConnectionChecks.ResolveAsync(UnresolvableHost, Timeout));

    [Fact]
    public async Task ResolveAsync_AnIpLiteral_Passes()
        => Assert.Equal(DsmCheckFailure.None, await DsmConnectionChecks.ResolveAsync(LoopbackAddress, Timeout));

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

        Assert.Equal(DsmCheckFailure.PortRefused, await DsmConnectionChecks.ConnectAsync(LoopbackAddress, port, Timeout));
    }

    #endregion

    #region Certificate

    [Fact]
    public async Task InspectCertificateAsync_ASelfSignedCertificate_IsNeverAccepted()
    {
        // The guarantee that the check observes validation instead of bypassing it
        await using var server = LocalTlsServer.Start(CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var (failure, certificate) = await DsmConnectionChecks.InspectCertificateAsync(NetworkConstants.Localhost, server.Port, Timeout);

        Assert.Equal(DsmCheckFailure.CertificateUntrusted, failure);
        Assert.NotNull(certificate);
        Assert.Contains(NetworkConstants.Localhost, certificate.Names);
    }

    [Fact]
    public async Task InspectCertificateAsync_ANameTheCertificateDoesNotCover_ReportsNameMismatch()
    {
        await using var server = LocalTlsServer.Start(CreateCertificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1)));

        var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(LoopbackAddress, server.Port, Timeout);

        Assert.Equal(DsmCheckFailure.CertificateNameMismatch, failure);
    }

    [Fact]
    public async Task InspectCertificateAsync_AnExpiredCertificate_ReportsExpired()
    {
        await using var server = LocalTlsServer.Start(CreateCertificate(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddDays(-1)));

        var (failure, _) = await DsmConnectionChecks.InspectCertificateAsync(NetworkConstants.Localhost, server.Port, Timeout);

        Assert.Equal(DsmCheckFailure.CertificateExpired, failure);
    }

    [Theory]
    [InlineData(SslPolicyErrors.None)]
    [InlineData(SslPolicyErrors.RemoteCertificateNotAvailable)]
    public void ClassifyCertificateErrors_WithoutACertificateToBlame_ReportsHandshakeFailed(SslPolicyErrors errors)
        => Assert.Equal(DsmCheckFailure.HandshakeFailed, DsmConnectionChecks.ClassifyCertificateErrors(errors, X509ChainStatusFlags.NoError));

    #endregion

    #region Helpers

    static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = RSA.Create();
        var request = new CertificateRequest(CertificateSubject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName(NetworkConstants.Localhost);
        request.CertificateExtensions.Add(names.Build());

        using var ephemeral = request.CreateSelfSigned(notBefore, notAfter);

        // A PKCS#12 round trip: macOS cannot serve TLS from the ephemeral key CreateSelfSigned returns
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pkcs12), null);
    }

    /// <summary>
    /// Serves one TLS handshake on loopback with the given certificate.
    /// </summary>
    sealed class LocalTlsServer(X509Certificate2 certificate) : IAsyncDisposable
    {
        readonly TcpListener _listener = Listen();

        Task _handshake = Task.CompletedTask;

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

        public static LocalTlsServer Start(X509Certificate2 certificate)
        {
            var server = new LocalTlsServer(certificate);
            server._handshake = server.ServeOneHandshakeAsync();

            return server;
        }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            await _handshake;
            certificate.Dispose();
        }

        static TcpListener Listen()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            return listener;
        }

        async Task ServeOneHandshakeAsync()
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync();
                await using var stream = new SslStream(client.GetStream());
                await stream.AuthenticateAsServerAsync(certificate);
            }
            catch (Exception ex) when (ex is AuthenticationException or IOException or SocketException or ObjectDisposedException)
            {
                // Expected: the client refuses the certificate and aborts the handshake
            }
        }
    }

    #endregion
}

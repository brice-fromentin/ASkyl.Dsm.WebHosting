using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Askyl.Dsm.WebHosting.Constants.Network;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

/// <summary>
/// A self-signed certificate that covers <c>localhost</c> only, shared by the tests that need a TLS peer.
/// </summary>
internal static class TestCertificates
{
    internal const string Subject = "CN=localhost";

    internal static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter, string subject = Subject, IPAddress? ipName = null)
    {
        using var key = RSA.Create();
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();

        if (ipName is null)
        {
            names.AddDnsName(NetworkConstants.Localhost);
        }
        else
        {
            names.AddIpAddress(ipName);
        }

        request.CertificateExtensions.Add(names.Build());

        using var ephemeral = request.CreateSelfSigned(notBefore, notAfter);

        // A PKCS#12 round trip: macOS cannot serve TLS from the ephemeral key CreateSelfSigned returns
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pkcs12), null);
    }
}

/// <summary>
/// Serves one TLS handshake on loopback with the given certificate.
/// </summary>
internal sealed class LocalTlsServer(X509Certificate2 certificate) : IAsyncDisposable
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

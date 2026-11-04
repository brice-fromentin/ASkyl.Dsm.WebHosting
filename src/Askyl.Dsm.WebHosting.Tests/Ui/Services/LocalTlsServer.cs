using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.Network;
using Microsoft.Extensions.Configuration;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Services;

/// <summary>
/// A self-signed certificate that covers <c>localhost</c> only, shared by the tests that need a TLS peer.
/// </summary>
internal static class TestCertificates
{
    internal const string Subject = "CN=localhost";

    internal static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        using var key = RSA.Create();
        var request = new CertificateRequest(Subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();

        names.AddDnsName(NetworkConstants.Localhost);
        request.CertificateExtensions.Add(names.Build());

        using var ephemeral = request.CreateSelfSigned(notBefore, notAfter);

        // A PKCS#12 round trip: macOS cannot serve TLS from the ephemeral key CreateSelfSigned returns
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pkcs12), null);
    }

    /// <summary>
    /// A configuration whose ADWH_DSM_CERT_SHA256 holds the given fingerprint, or nothing.
    /// </summary>
    internal static IConfiguration Pin(string? sha256)
        => new ConfigurationBuilder().AddInMemoryCollection([new(ApplicationConstants.DsmCertificateSha256EnvironmentVariable, sha256)]).Build();
}

/// <summary>
/// Serves TLS handshakes on loopback with the given certificate, one connection at a time until disposed, optionally
/// answering one HTTP request after each. The diagnostic connects twice: once to see the port answer, once for TLS.
/// </summary>
internal sealed class LocalTlsServer(X509Certificate2 certificate, bool answerHttp) : IAsyncDisposable
{
    const string EmptyOkResponse = "HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n";

    const string EndOfHeaders = "\r\n\r\n";

    readonly TcpListener _listener = Listen();

    // Ends a handshake or a read still waiting when the test is over: stopping the listener leaves accepted sockets
    // open, so a client that never speaks would otherwise hold the disposal forever
    readonly CancellationTokenSource _stopping = new();

    Task _handshake = Task.CompletedTask;

    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

    public static LocalTlsServer Start(X509Certificate2 certificate, bool answerHttp = false)
    {
        var server = new LocalTlsServer(certificate, answerHttp);
        server._handshake = server.ServeAsync();

        return server;
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        _listener.Stop();
        await _handshake;
        _stopping.Dispose();
        certificate.Dispose();
    }

    static TcpListener Listen()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return listener;
    }

    static async Task AnswerAsync(SslStream stream, CancellationToken cancellationToken)
    {
        var request = new StringBuilder();
        var buffer = new byte[1024];

        while (!request.ToString().Contains(EndOfHeaders, StringComparison.Ordinal))
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);

            if (read == 0)
            {
                return;
            }

            request.Append(Encoding.ASCII.GetString(buffer, 0, read));
        }

        await stream.WriteAsync(Encoding.ASCII.GetBytes(EmptyOkResponse), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    async Task ServeAsync()
    {
        while (true)
        {
            TcpClient client;

            try
            {
                client = await _listener.AcceptTcpClientAsync();
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or InvalidOperationException)
            {
                // Disposed: the listener stopped, during the wait or before it began (InvalidOperationException)
                return;
            }

            using (client)
            {
                await ServeOneHandshakeAsync(client);
            }
        }
    }

    async Task ServeOneHandshakeAsync(TcpClient client)
    {
        try
        {
            await using var stream = new SslStream(client.GetStream());
            await stream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = certificate }, _stopping.Token);

            if (answerHttp)
            {
                await AnswerAsync(stream, _stopping.Token);
            }
        }
        catch (Exception ex) when (ex is AuthenticationException or IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            // Expected: the client refuses the certificate and aborts the handshake, only checked that the port answers,
            // or never spoke before the test ended
        }
    }
}

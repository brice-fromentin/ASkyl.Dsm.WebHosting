using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// The network checks of the DSM connection diagnostic. Stateless; each is bounded by its timeout and
/// reports a failure kind instead of throwing.
/// </summary>
internal static class DsmConnectionChecks
{
    /// <summary>
    /// Resolves the hostname. An IP literal passes without a lookup.
    /// </summary>
    internal static async Task<DsmCheckFailure> ResolveAsync(string host, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellation.Token);

            return addresses.Length > 0 ? DsmCheckFailure.None : DsmCheckFailure.NameNotFound;
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException)
        {
            return DsmCheckFailure.NameNotFound;
        }
    }

    /// <summary>
    /// Opens a TCP connection to the port.
    /// </summary>
    internal static async Task<DsmCheckFailure> ConnectAsync(string host, int port, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        using var client = new TcpClient();

        try
        {
            await client.ConnectAsync(host, port, cancellation.Token);

            return DsmCheckFailure.None;
        }
        catch (OperationCanceledException)
        {
            return DsmCheckFailure.PortTimedOut;
        }
        catch (SocketException)
        {
            return DsmCheckFailure.PortRefused;
        }
    }

    /// <summary>
    /// Runs a TLS handshake against the hostname with default validation, recording what DSM presented.
    /// </summary>
    internal static async Task<(DsmCheckFailure Failure, DsmCertificateSummary? Certificate)> InspectCertificateAsync(string host, int port, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);

        DsmCertificateSummary? summary = null;
        var errors = SslPolicyErrors.None;
        var chainStatus = X509ChainStatusFlags.NoError;

        var options = new SslClientAuthenticationOptions
        {
            TargetHost = host,
            RemoteCertificateValidationCallback = (_, certificate, chain, policyErrors) =>
            {
                summary = certificate is X509Certificate2 presented ? Summarize(presented) : null;
                errors = policyErrors;
                chainStatus = chain?.ChainStatus.Aggregate(X509ChainStatusFlags.NoError, (all, status) => all | status.Status) ?? X509ChainStatusFlags.NoError;

                // Observes default validation and never relaxes it
                return policyErrors == SslPolicyErrors.None;
            }
        };

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellation.Token);

            await using var stream = new SslStream(client.GetStream());
            await stream.AuthenticateAsClientAsync(options, cancellation.Token);

            return (DsmCheckFailure.None, summary);
        }
        catch (Exception ex) when (ex is AuthenticationException or IOException or SocketException or OperationCanceledException)
        {
            return (ClassifyCertificateErrors(errors, chainStatus), summary);
        }
    }

    /// <summary>
    /// Names what a failed handshake reported. Called only after a failure, so no error at all means the
    /// handshake broke for a reason other than the certificate.
    /// </summary>
    internal static DsmCheckFailure ClassifyCertificateErrors(SslPolicyErrors errors, X509ChainStatusFlags chainStatus)
    {
        if (errors == SslPolicyErrors.None || errors.HasFlag(SslPolicyErrors.RemoteCertificateNotAvailable))
        {
            return DsmCheckFailure.HandshakeFailed;
        }

        if (errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
        {
            return DsmCheckFailure.CertificateNameMismatch;
        }

        return chainStatus.HasFlag(X509ChainStatusFlags.NotTimeValid) ? DsmCheckFailure.CertificateExpired : DsmCheckFailure.CertificateUntrusted;
    }

    static DsmCertificateSummary Summarize(X509Certificate2 certificate)
    {
        // DNS names only: the report never carries an IP address
        List<string> names = [.. certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().SelectMany(extension => extension.EnumerateDnsNames())];

        if (names.Count == 0)
        {
            names.Add(certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false));
        }

        return new(names, certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: true), new DateTimeOffset(certificate.NotAfter.ToUniversalTime()));
    }
}

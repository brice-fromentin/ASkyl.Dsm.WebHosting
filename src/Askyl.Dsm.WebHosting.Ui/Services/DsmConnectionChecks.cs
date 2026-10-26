using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;
using Askyl.Dsm.WebHosting.Tools.Network;

namespace Askyl.Dsm.WebHosting.Ui.Services;

/// <summary>
/// The network checks of the DSM connection diagnostic. Stateless; each reports a failure kind instead
/// of throwing, and each await stops at its timeout — WaitAsync included, because a name lookup (only with
/// ADWH_DSM_URL) ignores its token on Linux and macOS.
/// </summary>
internal static class DsmConnectionChecks
{
    /// <summary>
    /// Opens a TCP connection to the port.
    /// </summary>
    internal static async Task<DsmCheckFailure> ConnectAsync(string host, int port, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        using var client = new TcpClient();

        try
        {
            await client.ConnectAsync(host, port, cancellation.Token).AsTask().WaitAsync(cancellation.Token);

            return DsmCheckFailure.None;
        }
        catch (OperationCanceledException)
        {
            return DsmCheckFailure.PortTimedOut;
        }
        catch (SocketException ex)
        {
            // A host or network that cannot be reached is a firewall-shaped problem, which the timeout remedy names
            return ex.SocketErrorCode == SocketError.ConnectionRefused ? DsmCheckFailure.PortRefused : DsmCheckFailure.PortTimedOut;
        }
    }

    /// <summary>
    /// Runs a TLS handshake under the application's own certificate policy (default validation everywhere
    /// except a loopback IP literal with a root-owned listener).
    /// </summary>
    /// <returns>The failure, and the certificate errors the policy saw, for the log.</returns>
    internal static async Task<(DsmCheckFailure Failure, SslPolicyErrors Errors)> InspectCertificateAsync(string host, int port, DsmCertificatePolicy policy, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);

        var errors = SslPolicyErrors.None;
        var refused = false;

        var options = new SslClientAuthenticationOptions
        {
            TargetHost = host,
            RemoteCertificateValidationCallback = (_, _, _, policyErrors) =>
            {
                errors = policyErrors;

                // The same policy as the HttpClient that calls DSM, so the checklist matches reality
                refused = !policy.Accepts(host, port, policyErrors);

                return !refused;
            }
        };

        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellation.Token).AsTask().WaitAsync(cancellation.Token);

            await using var stream = new SslStream(client.GetStream());
            await stream.AuthenticateAsClientAsync(options, cancellation.Token).WaitAsync(cancellation.Token);

            return (DsmCheckFailure.None, errors);
        }
        catch (Exception ex) when (ex is AuthenticationException or IOException or SocketException or OperationCanceledException)
        {
            return (refused ? DsmCheckFailure.NotTrusted : DsmCheckFailure.HandshakeFailed, errors);
        }
    }
}

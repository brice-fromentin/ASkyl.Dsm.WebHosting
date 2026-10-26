using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Askyl.Dsm.WebHosting.Constants.Application;
using Microsoft.Extensions.Configuration;

namespace Askyl.Dsm.WebHosting.Tools.Network;

/// <summary>
/// The one certificate policy for reaching DSM. Default validation applies everywhere except a loopback
/// IP literal whose port is held by a root-owned listener: traffic to 127.0.0.1 never leaves the machine
/// (the DSM client uses no proxy), an IP literal cannot be redirected by name resolution, and an
/// unprivileged process cannot impersonate DSM's root-owned listener. A name, even <c>localhost</c>, is
/// never relaxed. The trust assumption: DSM is whoever root-owns the loopback listener on DSM's port. A stale
/// port fails closed unless another root-owned listener (say a reverse-proxy rule an administrator created)
/// now holds it, which is no escalation. The probe runs in the certificate callback, after the TCP connect,
/// so a small time-of-check window remains; <c>SocketsHttpHandler.ConnectCallback</c> could close it.
/// For local debugging, ADWH_DSM_CERT_SHA256 also trusts the one certificate whose fingerprint it names.
/// </summary>
/// <param name="probe">Tells whether a root-owned socket listens on a loopback address and port.</param>
/// <param name="configuration">Application configuration, which holds ADWH_DSM_CERT_SHA256.</param>
public sealed class DsmCertificatePolicy(IRootListenerProbe probe, IConfiguration configuration)
{
    readonly string? _pinnedSha256 = configuration[ApplicationConstants.DsmCertificateSha256EnvironmentVariable];

    public bool Accepts(Uri? uri, X509Certificate? certificate, SslPolicyErrors errors) => Accepts(uri?.Host, uri?.Port ?? 0, certificate, errors);

    public bool Accepts(string? host, int port, X509Certificate? certificate, SslPolicyErrors errors)
        => errors == SslPolicyErrors.None || IsPinned(certificate) || (IPAddress.TryParse(host?.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address) && probe.IsRootListening(address, port));

    /// <summary>
    /// The handler of the DSM HTTP client. DSM is this machine, so it never goes through a proxy (which
    /// would read the login body) and never follows a redirect (which would send that body elsewhere).
    /// </summary>
    public HttpClientHandler CreateHandler() => new()
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        ServerCertificateCustomValidationCallback = (request, certificate, _, errors) => Accepts(request.RequestUri, certificate, errors)
    };

    // The certificate itself is checked, so no name, issuer or date can stand in for it; no pin matches nothing
    bool IsPinned(X509Certificate? certificate)
        => certificate is not null && String.Equals(certificate.GetCertHashString(HashAlgorithmName.SHA256), _pinnedSha256, StringComparison.OrdinalIgnoreCase);
}

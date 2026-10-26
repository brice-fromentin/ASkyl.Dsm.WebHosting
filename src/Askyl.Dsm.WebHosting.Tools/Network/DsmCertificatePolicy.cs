using System.Net;
using System.Net.Security;

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
/// </summary>
/// <param name="probe">Tells whether a root-owned socket listens on a loopback address and port.</param>
public sealed class DsmCertificatePolicy(IRootListenerProbe probe)
{
    public bool Accepts(Uri? uri, SslPolicyErrors errors) => Accepts(uri?.Host, uri?.Port ?? 0, errors);

    public bool Accepts(string? host, int port, SslPolicyErrors errors)
        => errors == SslPolicyErrors.None || (IPAddress.TryParse(host?.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address) && probe.IsRootListening(address, port));

    /// <summary>
    /// The handler of the DSM HTTP client. DSM is this machine, so it never goes through a proxy (which
    /// would read the login body) and never follows a redirect (which would send that body elsewhere).
    /// </summary>
    public HttpClientHandler CreateHandler() => new()
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        ServerCertificateCustomValidationCallback = (request, _, _, errors) => Accepts(request.RequestUri, errors)
    };
}

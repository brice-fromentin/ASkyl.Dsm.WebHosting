using System.Net;
using System.Net.Security;

namespace Askyl.Dsm.WebHosting.Tools.Network;

/// <summary>
/// The one certificate policy for reaching DSM. Default validation applies everywhere except a loopback
/// IP literal: traffic to 127.0.0.1 never leaves the machine, and an IP literal cannot be redirected by
/// name resolution. A name, even <c>localhost</c>, is never relaxed because a resolver or hosts file
/// could point it elsewhere.
/// </summary>
public static class DsmCertificatePolicy
{
    public static bool Accepts(string? host, SslPolicyErrors errors) => errors == SslPolicyErrors.None || IsLoopbackLiteral(host);

    static bool IsLoopbackLiteral(string? host) => IPAddress.TryParse(host?.Trim('[', ']'), out var address) && IPAddress.IsLoopback(address);
}

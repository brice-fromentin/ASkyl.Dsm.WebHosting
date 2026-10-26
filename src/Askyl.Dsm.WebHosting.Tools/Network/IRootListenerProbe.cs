using System.Net;

namespace Askyl.Dsm.WebHosting.Tools.Network;

/// <summary>
/// Tells who owns the socket listening on a loopback address, so a port an unprivileged process could have
/// bound is never mistaken for DSM.
/// </summary>
public interface IRootListenerProbe
{
    /// <summary>
    /// True when a root-owned socket is listening on the loopback address (or on every address) at the port.
    /// </summary>
    bool IsRootListening(IPAddress loopbackAddress, int port);
}

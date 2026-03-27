namespace Askyl.Dsm.WebHosting.Constants.Network;

/// <summary>
/// Defines network protocol types for web application hosting.
/// </summary>
public enum ProtocolType
{
    /// <summary>
    /// HTTP (Hypertext Transfer Protocol) - unencrypted communication.
    /// Default port: 80
    /// </summary>
    HTTP = 0,

    /// <summary>
    /// HTTPS (HTTP Secure) - encrypted communication using TLS/SSL.
    /// Default port: 443
    /// </summary>
    HTTPS = 1
}

namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// The checks of the DSM connection diagnostic, in the order they run.
/// </summary>
public enum DsmConnectionCheck
{
    /// <summary>DSM accepts a TCP connection on its HTTPS port.</summary>
    PortAnswers,

    /// <summary>The TLS handshake passes the certificate policy of the DSM client.</summary>
    ConnectionTrusted,

    /// <summary>SYNO.API.Info answers with the API list.</summary>
    ApiAnswers
}

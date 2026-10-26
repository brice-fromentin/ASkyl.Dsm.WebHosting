namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// Why a diagnostic check failed. Carried as data so the client can localize it.
/// </summary>
public enum DsmCheckFailure
{
    /// <summary>No failure.</summary>
    None,

    /// <summary>The port refused the connection.</summary>
    PortRefused,

    /// <summary>The port did not answer in time.</summary>
    PortTimedOut,

    /// <summary>
    /// The certificate policy refused the connection: on the NAS, DSM does not hold the loopback port; from a
    /// developer machine, DSM's certificate does not pass validation.
    /// </summary>
    NotTrusted,

    /// <summary>The TLS handshake failed without the policy refusing it.</summary>
    HandshakeFailed,

    /// <summary>SYNO.API.Info did not answer with the API list.</summary>
    UnexpectedResponse,

    /// <summary>The check failed in a way it does not classify; the application log has the exception.</summary>
    Unexpected
}

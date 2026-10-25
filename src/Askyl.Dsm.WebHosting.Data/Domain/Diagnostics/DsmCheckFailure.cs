namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// Why a diagnostic check failed. Carried as data so the client can localize it.
/// </summary>
public enum DsmCheckFailure
{
    /// <summary>No failure.</summary>
    None,

    /// <summary>synoinfo.conf has no external_host_ip.</summary>
    HostnameMissing,

    /// <summary>synoinfo.conf could not be read.</summary>
    SettingsUnreadable,

    /// <summary>The hostname does not resolve.</summary>
    NameNotFound,

    /// <summary>The port refused the connection.</summary>
    PortRefused,

    /// <summary>The port did not answer in time.</summary>
    PortTimedOut,

    /// <summary>The certificate does not cover the hostname.</summary>
    CertificateNameMismatch,

    /// <summary>The certificate does not chain to a trusted authority.</summary>
    CertificateUntrusted,

    /// <summary>The certificate is outside its validity period.</summary>
    CertificateExpired,

    /// <summary>The TLS handshake failed without a certificate to blame.</summary>
    HandshakeFailed,

    /// <summary>SYNO.API.Info did not answer with the API list.</summary>
    UnexpectedResponse
}

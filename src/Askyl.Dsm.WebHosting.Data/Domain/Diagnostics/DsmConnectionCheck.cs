namespace Askyl.Dsm.WebHosting.Data.Domain.Diagnostics;

/// <summary>
/// The checks of the DSM connection diagnostic, in the order they run.
/// </summary>
public enum DsmConnectionCheck
{
    /// <summary>synoinfo.conf names a DSM hostname.</summary>
    HostnameConfigured,

    /// <summary>The hostname resolves from the NAS.</summary>
    NameResolves,

    /// <summary>DSM accepts a TCP connection on its HTTPS port.</summary>
    PortAnswers,

    /// <summary>DSM's certificate passes full validation for the hostname.</summary>
    CertificateCoversName,

    /// <summary>SYNO.API.Info answers with the API list.</summary>
    ApiAnswers
}

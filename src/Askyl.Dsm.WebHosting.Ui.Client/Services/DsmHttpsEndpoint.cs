namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// Tells whether the page was opened over plain HTTP on a host where the browser refuses the Secure session
/// cookies, and where the HTTPS version of the same page lives.
/// </summary>
public class DsmHttpsEndpoint(int httpsPort)
{
    // Browsers treat http://localhost as a secure context and keep Secure cookies there
    public bool IsInsecure(Uri pageUri) => pageUri.Scheme == Uri.UriSchemeHttp && !pageUri.IsLoopback;

    public Uri ToHttps(Uri pageUri) => new UriBuilder(pageUri) { Scheme = Uri.UriSchemeHttps, Port = httpsPort }.Uri;
}

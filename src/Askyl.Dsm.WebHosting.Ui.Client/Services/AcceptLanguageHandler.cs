using System.Net.Http.Headers;
using Askyl.Dsm.WebHosting.Data.Contracts;

namespace Askyl.Dsm.WebHosting.Ui.Client.Services;

/// <summary>
/// HTTP message handler that attaches the <c>Accept-Language</c> header
/// to all outgoing requests, propagating the WASM client's culture to the server.
/// The server uses <c>RequestLocalizationMiddleware</c> to read this header
/// and set the thread culture for localized responses.
/// </summary>
/// <param name="cultureManager">The culture manager providing the current culture.</param>
public class AcceptLanguageHandler(ICultureManager cultureManager) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.AcceptLanguage.Clear();
        request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(cultureManager.CurrentUICulture.Name));

        return base.SendAsync(request, cancellationToken);
    }
}

using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;

namespace Askyl.Dsm.WebHosting.Tests.Ui;

/// <summary>
/// The runtime gate. Format, warnings, blank lines and naming all pass whatever the application does when
/// it starts; P0-1 and P0-2 both shipped through a fully green build. These tests fail when the host
/// cannot build its service graph, its middleware pipeline, or its configuration.
/// </summary>
public class ApplicationStartupTests(ApplicationHostFactory factory) : IClassFixture<ApplicationHostFactory>
{
    const string BlazorBootScriptPrefix = "_framework/blazor.web.";

    static readonly string ProtectedRoute = String.Join("/", ApplicationConstants.ApplicationUrlSubPath, FileManagementRoutes.SharedFoldersFullRoute);

    [Fact]
    public async Task Application_Starts_AndServesTheRootPage()
    {
        // Constructing the client is what builds the host: a broken registration, a captive dependency or
        // a missing configuration key throws here rather than at a deployment.
        var client = factory.CreateClient();

        var response = await client.GetAsync(ApplicationConstants.ApplicationUrlSubPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Application_ServesTheRealHostPage_NotJustSomeHtml()
    {
        // A status 200 carrying text/html proves very little: an error page, an empty shell and a Blazor
        // host page whose assets failed to resolve all satisfy it. These two markers cannot be produced
        // by accident.
        var client = factory.CreateClient();

        var document = await new HtmlParser().ParseDocumentAsync(await client.GetStringAsync(ApplicationConstants.ApplicationUrlSubPath));

        // The path base reached the rendered markup, so relative asset URLs resolve behind DSM's proxy.
        Assert.Equal($"{ApplicationConstants.ApplicationUrlSubPath}/", document.QuerySelector("base")?.GetAttribute("href"));

        // A fingerprinted boot script can only come from a static asset manifest that actually resolved:
        // the hash is emitted by MapStaticAssets at build time, never by a hand-written fallback page.
        var bootScript = document.QuerySelectorAll("script")
                                 .Select(element => element.GetAttribute("src"))
                                 .FirstOrDefault(source => source is not null && source.Contains(BlazorBootScriptPrefix, StringComparison.Ordinal));

        Assert.NotNull(bootScript);
        Assert.Matches($@"{Regex.Escape(BlazorBootScriptPrefix)}[a-z0-9]+\.js$", bootScript);
    }

    [Fact]
    public async Task Application_ServesTheRootPage_WithSecurityHeaders()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(ApplicationConstants.ApplicationUrlSubPath);

        Assert.True(response.Headers.Contains(SecurityHeaders.ContentSecurityPolicyName));
        Assert.True(response.Headers.Contains(SecurityHeaders.XContentTypeOptionsName));
        Assert.True(response.Headers.Contains(SecurityHeaders.XFrameOptionsName));
    }

    [Fact]
    public async Task ProtectedApi_WithoutSession_DoesNotSucceed()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(ProtectedRoute);

        // Asserting "not success" rather than a status code on purpose. [AuthorizeSession] currently
        // surfaces as 500 because Program.cs registers no authentication scheme, which is recorded in
        // open-technical-items.md; this test pins the security property and survives the fix.
        Assert.False(response.IsSuccessStatusCode);
    }
}

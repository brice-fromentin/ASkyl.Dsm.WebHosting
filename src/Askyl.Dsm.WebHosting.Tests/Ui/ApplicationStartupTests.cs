using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Constants.WebApi;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;

namespace Askyl.Dsm.WebHosting.Tests.Ui;

/// <summary>
/// The runtime gate. Format, warnings, blank lines and naming all pass whatever the application does when
/// it starts; P0-1 and P0-2 both shipped through a fully green build. These tests fail when the host
/// cannot build its service graph, its middleware pipeline, or its configuration.
/// </summary>
public class ApplicationStartupTests(ApplicationHostFactory factory) : IClassFixture<ApplicationHostFactory>
{
    const string BlazorBootScriptPrefix = "_framework/blazor.web.";

    const string MissingPathSegment = "definitely-not-a-real-page";

    /// <summary>
    /// Any host but localhost, which <c>HstsOptions.ExcludedHosts</c> skips by default — addressing the
    /// test server by its usual name would hide the header whatever the pipeline does.
    /// </summary>
    const string ProxiedBaseAddress = "http://nas.example.test";

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
    public async Task MissingPath_ServesTheNotFoundPage_NotAnEmptyBody()
    {
        // Status code re-execution is invisible to ErrorEndpointsTests, which calls the handler directly.
        // Only a request through the real pipeline shows whether the middleware ever reaches it.
        var client = factory.CreateClient();

        var response = await client.GetAsync($"{ApplicationConstants.ApplicationUrlSubPath}/{MissingPathSegment}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        // The requested path can only appear in the body through IStatusCodeReExecuteFeature, so this
        // also pins that the re-execution carried it rather than losing it to the rewritten request.
        Assert.Contains(MissingPathSegment, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Application_SendsHsts_OnlyWhenTheProxyReportsHttps()
    {
        // DSM's nginx terminates TLS and forwards plain HTTP over loopback, so Request.IsHttps is false
        // unless X-Forwarded-Proto is honoured — and HstsMiddleware writes no header at all when it is
        // false. Without the XForwardedProto flag the application therefore ships HSTS to nobody, which
        // a status code or a body assertion can never reveal.
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new(ProxiedBaseAddress) });

        var forwarded = new HttpRequestMessage(HttpMethod.Get, ApplicationConstants.ApplicationUrlSubPath);
        forwarded.Headers.Add(ForwardedHeadersDefaults.XForwardedProtoHeaderName, Uri.UriSchemeHttps);

        var overHttps = await client.SendAsync(forwarded);
        var overHttp = await client.GetAsync(ApplicationConstants.ApplicationUrlSubPath);

        Assert.True(overHttps.Headers.Contains(HeaderNames.StrictTransportSecurity));

        // The negative half matters as much: a header sent unconditionally would pass the assertion
        // above while telling a plain-HTTP client to never speak plain HTTP again.
        Assert.False(overHttp.Headers.Contains(HeaderNames.StrictTransportSecurity));
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

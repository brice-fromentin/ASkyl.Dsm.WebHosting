using System.Net;
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

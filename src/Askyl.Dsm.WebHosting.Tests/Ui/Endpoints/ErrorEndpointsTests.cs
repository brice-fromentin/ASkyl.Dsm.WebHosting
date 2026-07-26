using Askyl.Dsm.WebHosting.Ui.Endpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Endpoints;

public class ErrorEndpointsTests
{
    const string MaliciousPath = "/<script>alert(1)</script>";

    [Fact]
    public void HandleStatusCode_EncodesOriginalPath_InHtmlResponse()
    {
        // Status code re-execution supplies OriginalPath as a raw, URL-decoded string — unlike
        // Request.Path, which is a PathString and renders percent-encoded. This is the path that
        // reaches the HTML body in production, so it is the one that must be encoded.
        var context = CreateContextWithReExecutedPath(MaliciousPath);

        var result = ErrorEndpoints.HandleStatusCode(context);

        var content = Assert.IsType<ContentHttpResult>(result);
        Assert.NotNull(content.ResponseContent);
        Assert.DoesNotContain("<script>", content.ResponseContent);
        Assert.Contains("&lt;script&gt;", content.ResponseContent);
    }

    [Fact]
    public void HandleStatusCode_ReturnsRequestedStatusCode()
    {
        var context = CreateContextWithReExecutedPath("/missing");
        context.Request.QueryString = new QueryString("?status=404");

        var result = ErrorEndpoints.HandleStatusCode(context);

        var content = Assert.IsType<ContentHttpResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, content.StatusCode);
    }

    static DefaultHttpContext CreateContextWithReExecutedPath(string originalPath)
    {
        var context = new DefaultHttpContext();

        context.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature
        {
            OriginalPath = originalPath,
            OriginalPathBase = String.Empty
        });

        return context;
    }
}

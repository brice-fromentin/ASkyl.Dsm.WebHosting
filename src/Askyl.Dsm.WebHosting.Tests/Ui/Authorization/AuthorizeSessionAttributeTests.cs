using Askyl.Dsm.WebHosting.Data.Contracts;
using Askyl.Dsm.WebHosting.Data.Results;
using Askyl.Dsm.WebHosting.Ui.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Askyl.Dsm.WebHosting.Tests.Ui.Authorization;

/// <summary>
/// The filter that gates every API controller in the application, and had no tests at all.
/// </summary>
public class AuthorizeSessionAttributeTests
{
    readonly Mock<IAuthenticationService> _authenticationService = new();

    AuthorizationFilterContext CreateContext(CancellationToken requestAborted = default)
    {
        var services = new ServiceCollection();

        services.AddSingleton(_authenticationService.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            RequestAborted = requestAborted
        };

        return new AuthorizationFilterContext(new ActionContext(httpContext, new RouteData(), new ActionDescriptor()), []);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithAValidSession_LetsTheRequestThrough()
    {
        // Arrange
        _authenticationService.Setup(a => a.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResultBool.CreateSuccess(true));

        var context = CreateContext();

        // Act
        await new AuthorizeSessionAttribute().OnAuthorizationAsync(context);

        // Assert — leaving Result unset is what lets the pipeline continue to the action.
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WithoutASession_RefusesWithUnauthorized()
    {
        // Arrange
        _authenticationService.Setup(a => a.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResultBool.CreateSuccess(false));

        var context = CreateContext();

        // Act
        await new AuthorizeSessionAttribute().OnAuthorizationAsync(context);

        // Assert — a status the caller can branch on. ForbidResult needs a registered authentication
        // scheme, which this application does not have, so it threw and surfaced as 500.
        var result = Assert.IsType<UnauthorizedResult>(context.Result);

        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task OnAuthorizationAsync_WhenValidationItselfFails_StillRefuses()
    {
        // Fails closed: a validation that could not complete is not permission to proceed.
        _authenticationService.Setup(a => a.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResultBool.CreateFailure("DSM unreachable"));

        var context = CreateContext();

        // Act
        await new AuthorizeSessionAttribute().OnAuthorizationAsync(context);

        // Assert
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_PassesTheRequestAbortedToken()
    {
        // Validation calls DSM over the network. A client that hangs up should not leave that call
        // running, and nothing else would notice if the token stopped being forwarded.
        using var cancellation = new CancellationTokenSource();

        var received = CancellationToken.None;

        _authenticationService.Setup(a => a.IsAuthenticatedAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => received = token)
            .ReturnsAsync(ApiResultBool.CreateSuccess(true));

        // Act
        await new AuthorizeSessionAttribute().OnAuthorizationAsync(CreateContext(cancellation.Token));

        // Assert
        Assert.Equal(cancellation.Token, received);
    }
}

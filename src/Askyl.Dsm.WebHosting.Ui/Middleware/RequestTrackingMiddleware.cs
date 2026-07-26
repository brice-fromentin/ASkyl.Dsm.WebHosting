using Askyl.Dsm.WebHosting.Constants.Application;

namespace Askyl.Dsm.WebHosting.Ui.Middleware;

/// <summary>
/// Propagates X-Request-ID header through the request pipeline.
/// </summary>
public sealed class RequestTrackingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[ApplicationConstants.XRequestIdHeaderName].FirstOrDefault() ?? Guid.NewGuid().ToString("N");
        context.Response.Headers[ApplicationConstants.XRequestIdHeaderName] = requestId;
        context.Items[ApplicationConstants.RequestIdItemKey] = requestId;

        await next(context);
    }
}

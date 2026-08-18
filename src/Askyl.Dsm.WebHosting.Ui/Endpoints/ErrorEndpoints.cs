using System.Net.Mime;
using System.Text.Encodings.Web;
using Askyl.Dsm.WebHosting.Constants.Application;
using Askyl.Dsm.WebHosting.Data.Results;
using Microsoft.AspNetCore.Diagnostics;

namespace Askyl.Dsm.WebHosting.Ui.Endpoints;

/// <summary>
/// Maps error handling endpoints for production exception and status code pages.
/// </summary>
public static class ErrorEndpoints
{
    /// <summary>
    /// Maps the /Error and /not-found endpoints to handle unhandled exceptions and 4xx status codes.
    /// </summary>
    public static void MapErrorEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/Error", HandleException);
        routes.MapGet(ApplicationConstants.NotFoundPagePath, HandleStatusCode);
    }

    static IResult HandleException(HttpContext context)
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var errorType = feature?.Error?.GetType().Name ?? "Unknown";
        var message = $"An unexpected error occurred: {errorType}";

        if (RequestAcceptsJson(context))
        {
            return TypedResults.Json(new ApiResult(false, message), statusCode: StatusCodes.Status500InternalServerError);
        }

        var html = $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"/><title>Error</title></head>
            <body><h1>An error occurred.</h1><p>{message}</p></body>
            </html>
            """;

        return TypedResults.Text(html, statusCode: StatusCodes.Status500InternalServerError, contentType: MediaTypeNames.Text.Html);
    }

    internal static IResult HandleStatusCode(HttpContext context)
    {
        var statusCode = int.TryParse(context.Request.Query[ApplicationConstants.NotFoundPageStatusParameter], out var code) ? code : StatusCodes.Status404NotFound;
        var originalPath = context.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath ?? context.Request.Path;

        if (RequestAcceptsJson(context))
        {
            return TypedResults.Json(new ApiResult(false, $"Resource not found: {originalPath}"), statusCode: statusCode);
        }

        // The path is caller-controlled and arrives URL-decoded, so it must be encoded before it
        // reaches an HTML response body.
        var encodedPath = HtmlEncoder.Default.Encode(originalPath);

        var html = $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"/><title>{statusCode} Not Found</title></head>
            <body><h1>{statusCode}</h1><p>The requested resource was not found: {encodedPath}</p></body>
            </html>
            """;

        return TypedResults.Text(html, statusCode: statusCode, contentType: MediaTypeNames.Text.Html);
    }

    static bool RequestAcceptsJson(HttpContext context)
        => context.Request.Headers.Accept.Any(header => header is not null && header.Contains(MediaTypeNames.Application.Json, StringComparison.OrdinalIgnoreCase));
}

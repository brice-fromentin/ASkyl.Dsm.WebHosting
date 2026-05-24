namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// HTTP security header values for response protection.
/// </summary>
public static class SecurityHeaders
{
    /// <summary>
    /// Prevents MIME type sniffing.
    /// </summary>
    public const string XContentTypeOptions = "nosniff";

    /// <summary>
    /// Prevents clickjacking via iframes.
    /// </summary>
    public const string XFrameOptions = "SAMEORIGIN";

    /// <summary>
    /// Limits referrer information sent to external origins.
    /// </summary>
    public const string ReferrerPolicy = "strict-origin-when-cross-origin";

    /// <summary>
    /// Restricts resource loading to same-origin with inline allowances for Blazor/FluentUI.
    /// Note: 'unsafe-eval' is required by Mono WASM to compile WebAssembly modules.
    /// </summary>
    public const string ContentSecurityPolicy = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:;";
}

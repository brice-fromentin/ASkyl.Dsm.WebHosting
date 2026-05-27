namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// HTTP security header names and values for response protection.
/// </summary>
public static class SecurityHeaders
{
    /// <summary>
    /// Header name for preventing MIME type sniffing.
    /// </summary>
    public const string XContentTypeOptionsName = "X-Content-Type-Options";

    /// <summary>
    /// Prevents MIME type sniffing.
    /// </summary>
    public const string XContentTypeOptions = "nosniff";

    /// <summary>
    /// Header name for preventing clickjacking via iframes.
    /// </summary>
    public const string XFrameOptionsName = "X-Frame-Options";

    /// <summary>
    /// Prevents clickjacking via iframes.
    /// </summary>
    public const string XFrameOptions = "SAMEORIGIN";

    /// <summary>
    /// Header name for limiting referrer information.
    /// </summary>
    public const string ReferrerPolicyName = "Referrer-Policy";

    /// <summary>
    /// Limits referrer information sent to external origins.
    /// </summary>
    public const string ReferrerPolicy = "strict-origin-when-cross-origin";

    /// <summary>
    /// Header name for restricting resource loading.
    /// </summary>
    public const string ContentSecurityPolicyName = "Content-Security-Policy";

    /// <summary>
    /// Restricts resource loading to same-origin with inline allowances for Blazor/FluentUI.
    /// Note: 'unsafe-eval' is required by Mono WASM to compile WebAssembly modules.
    /// </summary>
    public const string ContentSecurityPolicy = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:;";

    /// <summary>
    /// Header name for legacy XSS filter (Chrome/Edge legacy, Safari pre-16.4).
    /// </summary>
    public const string XXssProtectionName = "X-XSS-Protection";

    /// <summary>
    /// Enables legacy XSS filter. Note: Modern browsers (Chrome 87+, Edge 87+, Safari 16.4+) ignore this header.
    /// </summary>
    public const string XXssProtection = "1; mode=block";
}

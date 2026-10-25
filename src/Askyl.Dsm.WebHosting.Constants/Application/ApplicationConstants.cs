namespace Askyl.Dsm.WebHosting.Constants.Application;

/// <summary>
/// Defines application-specific constants for configuration and settings.
/// For website hosting constants, see <see cref="WebSiteConstants"/>.
/// </summary>
public static class ApplicationConstants
{
    #region Configuration Files

    /// <summary>
    /// Default name for the application settings file.
    /// </summary>
    public const string SettingsFileName = "appsettings.json";

    /// <summary>
    /// Configuration key for the download channel version setting.
    /// </summary>
    public const string ChannelVersionKey = "Download:ChannelVersion";

    /// <summary>
    /// Default configuration version.
    /// </summary>
    public const string DefaultConfigurationVersion = "1.0";

    #endregion

    #region HTTP Client

    /// <summary>
    /// HTTP client name identifier.
    /// </summary>
    public const string HttpClientName = "UiClient";

    /// <summary>
    /// HTTP client timeout in seconds. Must exceed <see cref="WebSiteConstants.DefaultProcessTimeoutSeconds"/> to avoid client-side timeouts during stop operations. SIGTERM + ASP.NET Core drain completes in ~1-3 seconds; 15s provides headroom for the full request/response roundtrip.
    /// </summary>
    public const int HttpClientTimeoutSeconds = 15;

    /// <summary>
    /// Time allowed to each network step of the DSM connection diagnostic. Four such steps run in sequence,
    /// and together they must fit inside <see cref="HttpClientTimeoutSeconds"/>, the client's wait for the result.
    /// </summary>
    public const int DsmDiagnosticStepTimeoutSeconds = 3;

    #endregion

    #region Runtime Path

    /// <summary>
    /// The root path for the .NET runtimes directory.
    /// </summary>
    public const string RuntimesRootPath = "../runtimes";

    #endregion

    #region Application Paths & Routing

    /// <summary>
    /// The sub-path for the application URL, used for routing.
    /// </summary>
    public const string ApplicationUrlSubPath = "/adwh";

    /// <summary>
    /// The home page path.
    /// </summary>
    public const string HomePagePath = "";

    /// <summary>
    /// The login page path.
    /// </summary>
    public const string LoginPagePath = "login";

    /// <summary>
    /// Path of the endpoint that renders the status code page, and the path the status code pages
    /// middleware re-executes the request against.
    /// </summary>
    public const string NotFoundPagePath = "/not-found";

    /// <summary>
    /// Query format handed to UseStatusCodePagesWithReExecute. It must stay separate from the path:
    /// the middleware assigns the path format to Request.Path verbatim, so a query embedded there
    /// becomes part of the path, matches no route, and the re-execution answers with an empty body.
    /// </summary>
    public const string NotFoundPageQueryFormat = "?status={0}";

    /// <summary>
    /// Query parameter carrying the original status code to the status code page.
    /// </summary>
    public const string NotFoundPageStatusParameter = "status";

    #endregion

    #region Session & Authentication

    /// <summary>
    /// Session key for DSM authentication SID.
    /// </summary>
    public const string DsmSessionKey = "DsmSid";

    /// <summary>
    /// Session key for the logged-in DSM username. Used by session validation to fetch user info via SYNO.Core.User.get.
    /// </summary>
    public const string DsmUsernameKey = "DsmUsername";

    /// <summary>
    /// Session idle timeout in minutes.
    /// </summary>
    public const int SessionTimeoutMinutes = 30;

    /// <summary>
    /// TTL for cached DSM session validation results in minutes. Matches the minimum DSM session timeout (1 minute) to prevent validating stale sessions. Prevents per-request API overhead while detecting expired sessions promptly.
    /// </summary>
    public const int SessionValidationTtlMinutes = 1;

    /// <summary>
    /// Prefix for session validation cache entries. The DSM SID is appended, which keys each entry to
    /// one user — a shared cache must never let one session's validity answer for another's.
    /// </summary>
    public const string SessionValidationCacheKeyPrefix = "dsm-session-valid:";

    /// <summary>
    /// Session key for the binding token issued at login. ASP.NET Core cannot rotate the session key, so
    /// the token is what ties a signed-in session to the browser that signed in.
    /// </summary>
    public const string SessionBindingKey = "SessionBinding";

    /// <summary>
    /// Cookie carrying the binding token. A planted session cookie shares the session key, never this value,
    /// which is issued afresh on every login.
    /// </summary>
    public const string SessionBindingCookieName = "ADWH.Binding";

    /// <summary>
    /// Number of random bytes in a binding token.
    /// </summary>
    public const int SessionBindingTokenBytes = 32;

    /// <summary>
    /// Rate limiting policy name for login endpoint throttling.
    /// </summary>
    public const string RateLimitPolicyLogin = "login-throttle";

    /// <summary>
    /// Rate limiting policy name for re-running the DSM connection diagnostic. Its own bucket, with the login
    /// partitioning, so a re-check never spends a login attempt.
    /// </summary>
    public const string RateLimitPolicyDiagnostic = "diagnostic-throttle";

    /// <summary>
    /// Login attempts allowed per client within <see cref="LoginRateLimitWindowMinutes"/>.
    /// </summary>
    public const int LoginRateLimitPermitLimit = 5;

    /// <summary>
    /// Length of the login rate limiting window, in minutes.
    /// </summary>
    public const int LoginRateLimitWindowMinutes = 1;

    /// <summary>
    /// Partition key used when the client address cannot be determined. Such requests share a single
    /// bucket, which throttles them collectively rather than leaving them unlimited.
    /// </summary>
    public const string RateLimitUnknownPartitionKey = "unknown-client";

    #endregion

    #region UI Interaction

    /// <summary>
    /// Double-click detection timeout in milliseconds for UI components.
    /// </summary>
    public const int DoubleClickTimeoutMilliseconds = 400;

    #endregion

    #region File Operations

    /// <summary>
    /// Filename used to test write permissions on a directory.
    /// </summary>
    public const string WriteTestFileName = ".write_test";

    #endregion

    #region Request Tracking

    /// <summary>
    /// HTTP header name for cross-cutting request identification.
    /// </summary>
    public const string XRequestIdHeaderName = "X-Request-ID";

    /// <summary>
    /// HttpContext.Items key for the request ID.
    /// </summary>
    public const string RequestIdItemKey = "RequestId";

    #endregion

    #region Build Version

    /// <summary>
    /// Separates the version number from the build metadata — the commit — in an informational version.
    /// </summary>
    public const char BuildMetadataSeparator = '+';

    /// <summary>
    /// Length of the abbreviated commit shown in the UI, as git abbreviates it by default.
    /// </summary>
    public const int ShortCommitLength = 7;

    /// <summary>
    /// Address of a commit in the public repository; the full commit hash is the argument.
    /// </summary>
    public const string RepositoryCommitUrlFormat = "https://github.com/brice-fromentin/ASkyl.Dsm.WebHosting/commit/{0}";

    #endregion
}

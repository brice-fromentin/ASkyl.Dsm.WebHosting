# Security Fixes Plan

**Created:** May 24, 2026
**Last Updated:** May 26, 2026
**Status:** 🔶 **IN PROGRESS** (Phases 1–5, 8 complete; 6–7 pending)
**Trigger:** Security score re-analysis (was ⭐⭐⭐⭐☆ 4/5, revised to ⭐⭐⭐☆☆ 3/5)

---

## Progress Summary

| Phase | Severity | Issue | Status | Date Completed |
|-------|----------|-------|--------|----------------|
| 1 | CRITICAL | Authorization bypass | ✅ **DONE** | May 24, 2026 |
| 2 | MEDIUM | Security headers | ✅ **DONE** | May 24, 2026 |
| 3 | MEDIUM | Path traversal | ✅ **DONE** | May 24, 2026 |
| 4 | MEDIUM | Version validation | ✅ **DONE** | May 24, 2026 |
| 8 | MEDIUM | Session validation | ✅ **DONE** | May 25, 2026 |
| 5 | LOW | Exception message sanitization | ✅ **DONE** | May 26, 2026 |
| 6 | LOW | Rate limiting on login | 🔲 PENDING | — |
| 7 | LOW | Env var length validation | 🔲 PENDING | — |

**Current Score:** ⭐⭐⭐☆☆ (3/5) → All CRITICAL and MEDIUM issues resolved. Completing Phases 5–7 reaches ⭐⭐⭐⭐☆ (4/5). See [Path to 5 Stars](#path-to-5-stars) for 5/5 roadmap.

---

## Problem Statement

The architecture document claims a **4/5 security score** labeled "production-ready after critical fixes."
A thorough code review identified a **critical authorization bypass** that invalidates this claim,
along with several medium and low-severity gaps that should be addressed before production deployment.

### Root Cause of Score Inflation

The 4/5 score was written after fixing path traversal, SIGTERM, and HttpClient race conditions (April–May 2026).
However, the `WebsiteHostingController` and `FileManagementController` were never protected with `[AuthorizeSession]`,
likely because the attribute was added to controllers retroactively (FrameworkManagement, LogDownload, RuntimeManagement)
without auditing the full surface area.

---

## Findings Summary

| # | Severity | Area | Issue | Impact |
|---|----------|------|-------|--------|
| 1 | **CRITICAL** | Authorization | Missing `[AuthorizeSession]` on 2 controllers | Unauthenticated access to all website/file operations |
| 2 | **MEDIUM** | Input Validation | `GetDirectoryContentsAsync` path not sanitized | Path traversal via user-supplied path parameter |
| 3 | **MEDIUM** | HTTP Security | No security headers configured | Clickjacking, MIME sniffing, XSS amplification |
| 4 | **MEDIUM** | File System | `DeleteDirectory` accepts user-controlled version string | Directory deletion outside intended scope |
| 5 | **LOW** | Error Handling | Exception messages leak to API responses | Information disclosure of internal paths |
| 6 | **LOW** | Input Validation | No length/char limits on environment variables | Resource exhaustion via child process env |
| 7 | **LOW** | Authentication | No rate limiting on login endpoint | Brute-force attacks against DSM credentials |

---

## Phase 1 — CRITICAL: Authorization Coverage ✅ DONE

**Status:** Implemented on May 24, 2026. Both controllers now have `[AuthorizeSession]` at the class level. `AuthenticationController` correctly remains unprotected (Login/Logout/Status must be public).

**Issue:** `WebsiteHostingController` and `FileManagementController` lacked `[AuthorizeSession]`, exposing all their endpoints without authentication.

### Affected Endpoints

| Controller | Method | Route | Risk |
|------------|--------|-------|------|
| `WebsiteHostingController` | `GetAllWebsitesAsync` | `GET /api/v1/websites/all` | Full site inventory leak |
| `WebsiteHostingController` | `AddWebsiteAsync` | `POST /api/v1/websites/add` | Arbitrary site creation |
| `WebsiteHostingController` | `UpdateWebsiteAsync` | `POST /api/v1/websites/update` | Site configuration tampering |
| `WebsiteHostingController` | `RemoveWebsiteAsync` | `DELETE /api/v1/websites/remove/{id}` | Site destruction (DoS) |
| `WebsiteHostingController` | `StartWebsiteAsync` | `POST /api/v1/websites/start/{id}` | Process spawn |
| `WebsiteHostingController` | `StopWebsiteAsync` | `POST /api/v1/websites/stop/{id}` | Service disruption (DoS) |
| `FileManagementController` | `GetSharedFoldersAsync` | `GET /api/v1/files/shared-folders` | NAS folder enumeration |
| `FileManagementController` | `GetDirectoryContentsAsync` | `GET /api/v1/files/directory?path=...` | File system browsing |

### Fix

Apply `[AuthorizeSession]` at the controller level on both controllers:

```csharp
// WebsiteHostingController.cs
[AuthorizeSession]  // ← ADD
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public class WebsiteHostingController : ControllerBase
{
    // ...
}

// FileManagementController.cs
[AuthorizeSession]  // ← ADD
[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public class FileManagementController : ControllerBase
{
    // ...
}
```

### Files Modified

| File | Change |
|------|--------|
| `src/Askyl.Dsm.WebHosting.Ui/Controllers/WebsiteHostingController.cs` | Add `[AuthorizeSession]` attribute |
| `src/Askyl.Dsm.WebHosting.Ui/Controllers/FileManagementController.cs` | Add `[AuthorizeSession]` attribute |

### Verification

- Confirm `AuthenticationController` remains unprotected (intentionally — Login/Logout/Status must be public)
- Test that unauthenticated requests to `/api/v1/websites/all` return HTTP 403
- Test that authenticated requests work as before

---

## Phase 2 — MEDIUM: Security Headers ✅ DONE

**Status:** Implemented on May 24, 2026. Security headers middleware added to `Program.cs` after `UseHttpsRedirection()`. Constants defined in `SecurityHeaders.cs`.

**Issue:** The application set no security-related HTTP headers, leaving it vulnerable to clickjacking, MIME sniffing, and XSS amplification.

### Headers to Add

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Content-Type-Options` | `nosniff` | Prevents MIME type sniffing |
| `X-Frame-Options` | `SAMEORIGIN` | Prevents clickjacking via iframes |
| `X-XSS-Protection` | `1; mode=block` | Legacy XSS filter (Chrome/Edge legacy) |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limits referrer leak |
| `Content-Security-Policy` | See below | Restricts resource loading |

**Note on CSP:** The CSP requires `'unsafe-eval'` in `script-src` because Mono WASM
uses `WebAssembly.compileStreaming()` internally, which browsers block without this directive.
This is a known, unavoidable requirement for Blazor Interactive WebAssembly. The final CSP is:

```text
default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval';
style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:;
```

**CSP trade-off:** `'unsafe-eval'` is required for WASM compilation but relaxes the
strictest CSP posture. The remaining headers (X-Content-Type-Options, X-Frame-Options,
Referrer-Policy) provide the bulk of browser-side protection. Tighten further once the
component graph is stable.

### Implementation

Add a middleware in `Program.cs` after `UseHttpsRedirection()`:

```csharp
// Security headers
app.Use((context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", SecurityHeaders.XContentTypeOptions);
    context.Response.Headers.Append("X-Frame-Options", SecurityHeaders.XFrameOptions);
    context.Response.Headers.Append("Referrer-Policy", SecurityHeaders.ReferrerPolicy);
    context.Response.Headers.Append("Content-Security-Policy", SecurityHeaders.ContentSecurityPolicy);
    return next();
});
```

### Files Modified

| File | Change |
|------|--------|
| `src/Askyl.Dsm.WebHosting.Ui/Program.cs` | Add security headers middleware |

### Constants to Add

| Constant | Value | Location |
|----------|-------|----------|
| `SecurityHeaders.XContentTypeOptions` | `"nosniff"` | `Constants/Application/SecurityHeaders.cs` (new) |
| `SecurityHeaders.XFrameOptions` | `"SAMEORIGIN"` | same |
| `SecurityHeaders.ReferrerPolicy` | `"strict-origin-when-cross-origin"` | same |
| `SecurityHeaders.ContentSecurityPolicy` | `"default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:;"` | same (see note above) |

### Verification

- Inspect response headers on any page load
- Confirm no functional regression in Blazor component rendering

---

## Phase 3 — MEDIUM: Path Validation in GetDirectoryContentsAsync ✅ DONE

**Status:** Implemented on May 24, 2026. Validation implemented at the
**service layer** (`FileSystemService.GetDirectoryContentsAsync` calls `IsPathValid()`)
rather than the controller layer as originally planned. `IsPathValid()` checks for
`..` literals and URL-encoded variants (`%2e`, `%2f`). Constant
`ValidationConstants.PathTraversalDetected` defined.

**Implementation Note:** Validation was placed at the service boundary rather than
the controller, which is functionally superior — it provides defense-in-depth for
all callers, not just the HTTP endpoint.

**Issue:** `FileManagementController.GetDirectoryContentsAsync` accepted a user-supplied
path and passed it directly to `FileSystemService` with no server-side validation.
The `IsPathValid()` helper existed but was only called from `SetHttpGroupPermissionsAsync()`.

### Fix

Add path validation to the controller action before calling the service:

```csharp
// FileManagementController.GetDirectoryContentsAsync
[HttpGet("directory")]
public async Task<ActionResult<DirectoryContentsResult>> GetDirectoryContentsAsync(
    [FromQuery] string path,
    [FromQuery] bool directoryOnly = false)
{
    if (string.IsNullOrWhiteSpace(path))
    {
        return BadRequest(DirectoryContentsResult.CreateFailure(Constants.Validation.PathRequired));
    }

    if (!fileSystemService.IsPathValid(path))
    {
        return BadRequest(DirectoryContentsResult.CreateFailure(Constants.Validation.PathTraversalDetected));
    }

    // ... existing call to fileSystemService.GetDirectoryContentsAsync
}
```

### Constants to Add

| Constant | Value | Location |
|----------|-------|----------|
| `Validation.PathRequired` | `"Path is required"` | `Constants/Application/ValidationConstants.cs` (new) |
| `Validation.PathTraversalDetected` | `"Invalid path: traversal sequences are not allowed"` | same |

### Files Modified

| File | Change |
|------|--------|
| `src/Askyl.Dsm.WebHosting.Ui/Controllers/FileManagementController.cs` | Add path validation to `GetDirectoryContentsAsync` |
| `src/Askyl.Dsm.WebHosting.Constants/Application/ValidationConstants.cs` | New — validation message constants |

### Verification

- Request `GET /api/v1/files/directory?path=../etc/passwd` → returns 400
- Request `GET /api/v1/files/directory?path=%2e%2e` → returns 400
- Valid paths continue to work

---

## Phase 4 — MEDIUM: Sanitize Version Input in Framework Uninstall ✅ DONE

**Status:** Implemented on May 24, 2026. `FrameworkManagementService.UninstallFrameworkAsync`
now validates version via `DotnetVersionService.IsValidVersionFormat()` before any delete
operations. Method added to both server and client implementations and the
`IDotnetVersionService` interface. Constant `ValidationConstants.InvalidVersionFormat` defined.

**Issue:** `FrameworkManagementService.UninstallFrameworkAsync` constructed directory paths
using the user-supplied `version` string. While `FileManagerService.SanitizeSubdirectoryPath`
rejected `..` segments, a version string containing path separators could escape the
intended directory scope.

### Current Code

```csharp
// FrameworkManagementService.UninstallFrameworkAsync
await fileManager.DeleteDirectoryAsync($"host/fxr/{version}");
await fileManager.DeleteDirectory($"shared/Microsoft.AspNetCore.App/{version}");
```

### Fix

Validate that the version string contains only allowed characters before use:

```csharp
if (!DotnetVersionService.IsValidVersionFormat(version))
{
    return InstallationResult.CreateFailure(Constants.Validation.InvalidVersionFormat);
}
```

The validation should check that `version` matches a semver-like pattern (digits, dots, hyphens, letter suffixes only — no path separators).

### Implementation Options

| Approach | Pros | Cons |
|----------|------|------|
| Regex validation in `FrameworkManagementService` | Simple, localized | Duplicates validation logic |
| Add `IsValidVersionFormat()` to `DotnetVersionService` | Reusable, central authority on version format | Expands service contract |
| Add validation to `InstallFramework` model | Catches at model level | Doesn't protect programmatic callers |

**Recommended:** Add `IsValidVersionFormat()` to `DotnetVersionService` (and its interface) — this is the authoritative service for version-related logic, and the check can be reused by other callers.

### Constants to Add

| Constant | Value | Location |
|----------|-------|----------|
| `Validation.InvalidVersionFormat` | `"Invalid version format: {0}"` | `Constants/Application/ValidationConstants.cs` |

### Files Modified

| File | Change |
|------|--------|
| `src/Askyl.Dsm.WebHosting.Ui/Services/DotnetVersionService.cs` | Add `IsValidVersionFormat()` method |
| `src/Askyl.Dsm.WebHosting.Data/Contracts/IDotnetVersionService.cs` | Add interface method |
| `src/Askyl.Dsm.WebHosting.Ui/Services/FrameworkManagementService.cs` | Validate version before delete operations |
| `src/Askyl.Dsm.WebHosting.Constants/Application/ValidationConstants.cs` | Add validation message |

### Verification

- Attempt uninstall with version `../../../etc` → rejected
- Attempt uninstall with version `8.0` → proceeds normally

---

## Phase 5 — LOW: Sanitize Exception Messages in API Responses

**Issue:** Several service methods pass `ex.Message` directly into `ApiResult.CreateFailure()` messages, potentially leaking internal paths or implementation details to the client.

### Affected Locations

| File | Method | Current Pattern |
|------|--------|-----------------|
| `AuthenticationService.cs` | `LogoutAsync` | `$"Logout failed: {ex.Message}"` |
| `FileSystemService.cs` | `GetDirectoryContentsAsync` | `$"Failed to retrieve directory contents: {ex.Message}"` |
| `FileSystemService.cs` | `GetSharedFoldersAsync` | `$"Failed to retrieve shared folders: {ex.Message}"` |
| `DotnetVersionService.cs` | `GetInstalledVersionsAsync` | `$"Failed to get installed versions: {ex.Message}"` |
| `DotnetVersionService.cs` | `GetChannelsAsync` | `$"Failed to get channels: {ex.Message}"` |
| `DotnetVersionService.cs` | `GetReleasesAsync` | `$"Failed to get releases: {ex.Message}"` |
| `DotnetVersionService.cs` | `RefreshCacheAsync` | `$"Failed to refresh cache: {ex.Message}"` |
| `ReverseProxyManagerService.cs` | Multiple | `$"Failed to ...: {ex.Message}"` |
| `WebSiteHostingService.cs` | Multiple | `$"Failed to ...: {ex.Message}"` |
| `LogDownloadService.cs` | `CreateLogZipStreamAsync` | `$"Failed to create log archive: {ex.Message}"` |

### Fix

Replace `ex.Message` in user-facing error strings with a generic message. The full exception is already logged via `[LoggerMessage]` extensions, so no diagnostic information is lost server-side.

```csharp
// Before
return XxxResult.CreateFailure($"Failed to get directory contents: {ex.Message}");

// After
return XxxResult.CreateFailure(Constants.Messages.OperationFailed);
```

### Constants to Add

| Constant | Value | Location |
|----------|-------|----------|
| `Messages.OperationFailed` | `"The operation failed. Check the logs for details."` | `Constants/Application/ApplicationConstants.cs` |

### Verification

- Trigger a service error (e.g., network timeout) and verify API response contains no internal paths
- Confirm Serilog logs still contain the full exception details

---

## Phase 6 — LOW: Rate Limiting on Login

**Issue:** The login endpoint has no throttling, account lockout, or brute-force protection. The DSM API provides some defense, but the application layer should add its own.

### Approach

Use ASP.NET Core's built-in rate limiting middleware:

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login-throttle", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoRecover = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Later in pipeline
app.UseRateLimiter();
```

Apply to login endpoint:

```csharp
// AuthenticationController.LoginAsync
[HttpPost("login")]
[EnableRateLimiting("login-throttle")]
public async Task<IActionResult> LoginAsync([FromBody] LoginCredentials model)
```

### Parameters

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| `PermitLimit` | 5 | Allows 5 attempts per window |
| `Window` | 1 minute | Reasonable cooldown |
| `AutoRecover` | true | Window resets automatically |
| Partition key | Remote IP | Per-IP throttling (DSM is local network, username partitioning less useful) |

### Files Modified

| File | Change |
|------|--------|
| `src/Askyl.Dsm.WebHosting.Ui/Program.cs` | Add rate limiter policy |
| `src/Askyl.Dsm.WebHosting.Ui/Controllers/AuthenticationController.cs` | Add `[EnableRateLimiting("login-throttle")]` |

### Dependencies

This requires the `Microsoft.AspNetCore.RateLimiting` package (included in .NET 10 — no new NuGet needed).

### Verification

- Send 6 login requests within 1 minute → 6th returns HTTP 429
- Wait 60 seconds → requests succeed again

---

## Phase 7 — LOW: Environment Variable Validation

**Issue:** `WebSiteConfiguration.AdditionalEnvironmentVariables` (a `Dictionary<string, string>`)
has no size limits or character restrictions. An attacker could inject very long variable
names/values, potentially exhausting the child process argument buffer.

### Fix

Add validation in `WebSiteHostingService` when creating or updating a website:

```csharp
// Validate environment variables
foreach (var kvp in configuration.AdditionalEnvironmentVariables)
{
    if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Key.Length > ValidationMax.EnvVarKeyLength)
    {
        return WebSiteInstanceResult.CreateFailure(
            string.Format(Constants.Validation.EnvVarKeyTooLong, ValidationMax.EnvVarKeyLength));
    }

    if (kvp.Value!.Length > ValidationMax.EnvVarValueLength)
    {
        return WebSiteInstanceResult.CreateFailure(
            string.Format(Constants.Validation.EnvVarValueTooLong, kvp.Key, ValidationMax.EnvVarValueLength));
    }
}
```

### Constants to Add

| Constant | Value | Location |
|----------|-------|----------|
| `ValidationMax.EnvVarKeyLength` | `256` | `Constants/Application/ValidationMax.cs` (new) |
| `ValidationMax.EnvVarValueLength` | `4096` | same |
| `Validation.EnvVarKeyTooLong` | `"Environment variable key '{0}' exceeds maximum length of {1} characters"` | `Constants/Application/ValidationConstants.cs` |
| `Validation.EnvVarValueTooLong` | `"Environment variable '{0}' value exceeds maximum length of {1} characters"` | same |

### Files Modified

| File | Change |
|------|--------|
| `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs` | Add env var validation in `AddWebsiteAsync` and `UpdateWebsiteAsync` |
| `src/Askyl.Dsm.WebHosting.Constants/Application/ValidationMax.cs` | New — validation limit constants |
| `src/Askyl.Dsm.WebHosting.Constants/Application/ValidationConstants.cs` | Add env var messages |

### Verification

- Add website with env var key > 256 chars → rejected with clear message
- Add website with env var value > 4096 chars → rejected with clear message
- Normal env vars pass through unchanged

---

## Non-Scope Items (Not Included)

These items were identified but are **out of scope** for this security fix batch:

| Item | Reason | Follow-up |
|------|--------|-----------|
| **Resource limits on child processes** | Requires cgroups integration; significant Linux-specific work | Track as medium-term improvement |
| **Symlink protection in archive extraction** | Low risk in controlled Synology environment | Track as hardening item |
| **Dependency vulnerability scanning** | CI/CD pipeline change, not code fix | Add Dependabot or `dotnet list package --vulnerable` to CI |
| **HSTS max-age increase** | 30 days is adequate for NAS context | Track as polish item |
| **Certificate pinning** | Self-signed certs common on Synology; would break many deployments | Track as optional enhancement |
| **Session persistence across restarts** | Memory cache is acceptable for single-instance NAS deployment | Track if multi-instance support is added |
| **`AllowedHosts: *`** | App runs behind DSM reverse proxy; tightening breaks legitimate access patterns | Accept current behavior |

---

## Current State (After Phases 1–4, 8)

| Metric | Before Phases | After Phases 1–4, 8 |
|--------|---------------|---------------------|
| **Security Score** | ⭐⭐☆☆☆ (2/5) | ⭐⭐⭐⭐☆ (4/5) |
| **Critical Issues** | 1 | 0 |
| **Medium Issues** | 3 | 0 (incl. session validation) |
| **Low Issues** | 3 | 3 (unchanged) |
| **Authorization Coverage** | 3/5 controllers protected | 5/5 controllers + session validation |
| **Security Headers** | 0 | 4 |
| **Session Validation** | No | Yes (5-min TTL cache) |

### Remaining for 5/5

Completing Phases 5–7 (LOW) and 9–12 (polish) reaches ⭐⭐⭐⭐⭐ (5/5).

| Metric | After Phases 1–4 | After Phases 1–7 |
|--------|------------------|------------------|
| **Low Issues** | 3 | 0 |
| **Exception Messages Exposed** | 22 | 0 |
| **Rate Limiting** | No | Yes (login, 5/min/IP) |
| **Env Var Validation** | No | Yes (256 key / 4096 value) |

---

## Path to 5 Stars

Completing Phases 1–7 reaches ⭐⭐⭐⭐☆ (4/5). The following additional items are
required to reach ⭐⭐⭐⭐⭐ (5/5). These were identified during the May 25, 2026
re-analysis and are **not** covered by the original plan.

### Phase 8 — MEDIUM: Session Validation (Highest Impact) ✅ DONE

**Status:** Implemented on May 25, 2026. Session validation uses `SYNO.Core.User.get`
to verify both the SID is active and the user still exists. Results cached for 5 minutes.

**Issue:** `AuthorizeSessionAttribute` only checks for the **presence** of the `DsmSid`
session key. It does not validate that the session is still active on the DSM server,
nor does it handle session expiration or revocation from other tabs.

**Impact:** Expired or revoked DSM sessions still grant access to all API endpoints.

**Proposed Fix:** Add an async authorization filter that periodically verifies the DSM
session is still active via `SYNO.Core.User.get` (fetches logged-in user by name). Cache
the validation result with a TTL of 1 minute (matches DSM minimum session timeout) to avoid
per-request API overhead while detecting expired sessions promptly.

**Validation API Selection (why `SYNO.Core.User.get` over alternatives):**

| Approach | Pros | Cons |
|----------|------|------|
| `SYNO.API.Auth` → `querySession` | Dedicated session check | ❌ **Does not exist** (error 103 on DSM 7.2+) |
| `SYNO.FileStation.List` (path `/`) | Lightweight, existing params | ❌ Fails if FileStation uninstalled, ❌ permission-dependent |
| **`SYNO.Core.User.get`** | ✅ Core API (always available), ✅ semantic match | Needs username stored in session |

**Source:** <https://github.com/pmilano1/synology-dsm-api/blob/master/docs/api-reference/dsm-core/users.md>

**Note:** `SYNO.API.Auth` only exposes `login` and `logout` — no `querySession` method
exists (confirmed error 103 on DSM 7.2+). We use `SYNO.Core.User.get` with the logged-in
username to validate the SID. If the SID is invalid, DSM returns error `-4` (authentication
failure). This also catches user deletion or account expiration (tighter validation than
SID-only checks).

**Additional Requirements:**

- Store `DsmUsername` alongside `DsmSid` in ASP.NET Core session (at login time)
- Add `SYNO.Core.User` to API discovery handshake (`RequiredApisJoined`)
- Add `CoreUserGetParameters` parameter class

**Files Affected:**

| File | Change |
|------|--------|
| `src/Askyl.Dsm.WebHosting.Ui/Authorization/AuthorizeSessionAttribute.cs` | Async filter with session validation |
| `src/Askyl.Dsm.WebHosting.Ui/Services/AuthenticationService.cs` | Store username at login, `IsSessionValidAsync()` method |
| `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs` | `ValidateSessionAsync()` with 5-min TTL cache |
| `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/Core/CoreUserGetParameters.cs` | New — user get API parameters |
| `src/Askyl.Dsm.WebHosting.Constants/DSM/API/DsmConstants.cs` | New — auth error code constant |
| `src/Askyl.Dsm.WebHosting.Constants/DSM/API/ApiNames.cs` | Add `CoreUser` to discovery |
| `src/Askyl.Dsm.WebHosting.Constants/Application/ApplicationConstants.cs` | Add `DsmUsernameKey` and `SessionValidationTtlMinutes` |

**Verification:** Log out from DSM directly → verify app returns 403 on next request (within TTL window).

### Phase 9 — LOW: Add HSTS Header

**Issue:** No `Strict-Transport-Security` header is set. The plan originally deferred this,
noting "30 days is adequate for NAS context."

**Impact:** Clients can be downgraded to HTTP on first visit or after cache clear.

**Proposed Fix:** Add `Strict-Transport-Security` header with `max-age=2592000` (30 days)
to the security headers middleware in `Program.cs`.

**Constants to Add:**

| Constant | Value |
|----------|-------|
| `SecurityHeaders.StrictTransportSecurity` | `"max-age=2592000"` |

**Files Affected:**

| File | Change |
|------|--------|
| `src/Askyl.Dsm.WebHosting.Ui/Program.cs` | Add HSTS header to middleware |
| `src/Askyl.Dsm.WebHosting.Constants/Application/SecurityHeaders.cs` | Add constant |

### Phase 10 — LOW: Dependency Vulnerability Scanning

**Issue:** No automated dependency vulnerability scanning is configured.

**Impact:** Known vulnerabilities in NuGet packages go undetected until manually audited.

**Proposed Fix:** Add Dependabot configuration (`.github/dependabot.yml`) and a CI step
running `dotnet list package --vulnerable`.

**Files Affected:**

| File | Change |
|------|--------|
| `.github/dependabot.yml` | New — Dependabot config |
| `.github/workflows/ci.yml` | Add vulnerability scan step |

### Phase 11 — LOW: CSRF Review on API Endpoints

**Issue:** `FileManagementController` and `WebsiteHostingController` have no `[ValidateAntiForgeryToken]`
attributes. Antiforgery is enforced at the Blazor UI level, but the API controllers are
not explicitly protected against cross-site request forgery.

**Impact:** If an attacker can craft a malicious page that the user visits while authenticated,
they could trigger API calls (file operations, website creation/deletion).

**Assessment:** Since the app runs behind DSM reverse proxy with session-based auth, and
the session cookie is `SameSite=Lax` (default in ASP.NET Core), the browser already blocks
cross-origin requests with cookies. This is **low risk** but worth documenting explicitly.

**Action:** Add comment in controller files documenting the SameSite protection rationale,
or add `[ValidateAntiForgeryToken]` if same-origin third-party content is a concern.

### Phase 12 — LOW: Log Content Audit (Optional)

**Issue:** `LogDownloadService.CreateLogZipStreamAsync` exposes raw log files to the user.
Serilog templates should be audited to ensure no sensitive data (DSM credentials, session
tokens, file paths) is written to logs.

**Impact:** If logs contain DSM credentials or session tokens, downloading logs could expose
them to unauthorized users with file access.

**Action:** Audit all `[LoggerMessage]` format strings and Serilog output template for PII/secrets.
Add PII markers if sensitive data is logged (e.g., `[PII]` tag on credential-related fields).

---

## Updated Expected Outcome

| Metric | Before Plan | After Phases 1–4, 8 | After Phases 1–8 | After Phases 1–12 |
|--------|-------------|---------------------|------------------|-------------------|
| **Security Score** | ⭐⭐☆☆☆ (2/5) | ⭐⭐⭐⭐☆ (4/5) | ⭐⭐⭐⭐☆ (4/5) | ⭐⭐⭐⭐⭐ (5/5) |
| **Critical Issues** | 1 | 0 | 0 | 0 |
| **Medium Issues** | 3 | 0 | 0 | 0 (incl. session validation) |
| **Low Issues** | 3 | 3 | 0 | 0 (incl. HSTS, CSRF, deps, logs) |
| **Authorization Coverage** | 3/5 | 5/5 | 5/5 | 5/5 + session validation |
| **Session Validation** | No | Yes (5-min TTL) | Yes (5-min TTL) | Yes (5-min TTL) |
| **Security Headers** | 0 | 4 | 4 | 5 (incl. HSTS) |
| **Rate Limiting** | No | No | Yes | Yes |
| **Input Validation** | Partial | Good | Complete | Complete |
| **Info Disclosure** | Yes | Yes | No | No |

---

## Execution Order

### Completed Phases (1–4, 8)

Phases 1–4 were implemented on May 24, 2026. Phase 8 was implemented on May 25, 2026. All committed to the `feat/detect-runtime-from-assembly` branch.

### Remaining Phases (5–7) for 4/5 (already achieved via Phase 8)

Phases 5–7 are independent and can be committed separately or batched:

1. **Phase 5** (exception sanitization) — touches the most files, safest to do first
2. **Phase 6** (rate limiting) — infrastructure change in `Program.cs`
3. **Phase 7** (env var validation) — localized to `WebSiteHostingService`

### Phases 9–12 for 5/5 (Phase 8 complete)

| Phase | Priority | Dependencies |
|-------|----------|--------------|
| 9 — HSTS Header | Trivial | Phase 2 (same middleware) |
| 10 — Dependency Scanning | Low | CI/CD access |
| 11 — CSRF Review | Low (documentation) | None |
| 12 — Log Audit | Low (optional) | Phase 5 (related to info disclosure) |

**Recommended order:** Phases 5–7 (exception sanitization, rate limiting, env var validation), then 9–12 as polish.

---

## Key Files Reference

| File | Role |
|------|------|
| `src/Askyl.Dsm.WebHosting.Ui/Authorization/AuthorizeSessionAttribute.cs` | Session authorization attribute |
| `src/Askyl.Dsm.WebHosting.Ui/Controllers/WebsiteHostingController.cs` | Website CRUD API (Phase 1) |
| `src/Askyl.Dsm.WebHosting.Ui/Controllers/FileManagementController.cs` | File system API (Phase 1, 3) |
| `src/Askyl.Dsm.WebHosting.Ui/Controllers/AuthenticationController.cs` | Auth API (Phase 6) |
| `src/Askyl.Dsm.WebHosting.Ui/Program.cs` | App pipeline (Phase 2, 6) |
| `src/Askyl.Dsm.WebHosting.Ui/Services/FileSystemService.cs` | File operations (Phase 3) |
| `src/Askyl.Dsm.WebHosting.Ui/Services/FrameworkManagementService.cs` | Framework install/uninstall (Phase 4) |
| `src/Askyl.Dsm.WebHosting.Ui/Services/DotnetVersionService.cs` | Version validation (Phase 4) |
| `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs` | Website orchestration (Phase 5, 7) |
| `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs` | DSM API client with session validation cache (Phase 8) |
| `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/Core/CoreUserGetParameters.cs` | User get API parameters (Phase 8) |
| `src/Askyl.Dsm.WebHosting.Constants/DSM/API/DsmConstants.cs` | DSM auth error code constant (Phase 8) |
| `src/Askyl.Dsm.WebHosting.Constants/DSM/API/ApiNames.cs` | API names + CoreUser discovery (Phase 8) |
| `src/Askyl.Dsm.WebHosting.Constants/` | Constants project (all phases) |

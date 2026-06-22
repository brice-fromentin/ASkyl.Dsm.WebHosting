# DsmApiClient Refactoring Plan

## Status: Complete

## Root Cause Analysis

`DsmApiClient` is a Singleton that combines three distinct responsibilities:

1. **HTTP API client** — request execution, serialization, logging, response parsing
2. **Per-user session state** — `Sid`, `_sessionValid`, `_lastSessionValidation`, user preferences
3. **System settings reader** — `ReadSettings()` parses `/etc/synoinfo.conf`

Additionally, the SID is stored in **two places**:

- `ISession` (per-user, correct) — `AuthenticationService` writes `DsmSid` and `DsmUsername` at login
- `DsmApiClient.Sid` (singleton, shared) — `SetSid()` mutates the singleton and its `DefaultRequestHeaders`

`DsmApiClient` never reads the SID back from `ISession`. It only writes to `ISession` at login time, then operates exclusively on its own singleton `Sid` field for all API calls.

This creates two categories of problems:

- **Security:** Session state from one user can leak into another user's requests
  when concurrent logins occur. The `DefaultRequestHeaders` dictionary is not
  thread-safe for concurrent writes, and the `SetSid()` Remove/Add sequence is not atomic.
- **Coupling:** System settings (read once at startup, never change) are coupled to
  a class that performs per-request, per-user operations. The `ReadSettings()` method
  throws at construction time, crashing the app if `/etc/synoinfo.conf` is missing.

---

## Current Session Flow (Before Refactoring)

### Login (`AuthenticationService.LoginAsync`)

1. Call `apiClient.ConnectAsync(model)` — authenticates against DSM, calls `SetSid()` on singleton
2. `SetSid()` mutates `DsmApiClient.Sid` field and `HttpClient.DefaultRequestHeaders` cookie
3. Fetch user preferences (`UserLanguage`, `UserDateFormat`, `UserTimeFormat`) on singleton
4. Write `DsmSid` and `DsmUsername` to `ISession` (correct, per-user)

### Validation (`AuthenticationService.IsAuthenticatedAsync`)

1. Read `DsmSid` and `DsmUsername` from `ISession` (correct, per-user)
2. Call `apiClient.ValidateSessionAsync(username)` — uses singleton `Sid` field, not `ISession`
3. TTL cache (`_sessionValid`, `_lastSessionValidation`) is shared across all users

### Logout (`AuthenticationService.LogoutAsync`)

1. Remove `DsmSid` and `DsmUsername` from `ISession`
2. Call `apiClient.DisconnectAsync()` — clears singleton `Sid`, cache, and user preferences

### The Bug

`ISession` correctly isolates per-user state, but `DsmApiClient` bypasses it entirely.
The singleton holds `Sid`, `_sessionValid`, `_lastSessionValidation`, and user preferences
as mutable fields. When user A and user B log in concurrently:

- User B's `SetSid()` overwrites user A's cookie in `DefaultRequestHeaders`
- User A's in-flight requests carry user B's session cookie
- The TTL cache is shared — user B's validation result affects user A

---

## Proposed Architecture

Split into three focused components:

```text
DsmSettingsService (Singleton)          — reads /etc/synoinfo.conf once at startup
DsmApiClient (Singleton)                — pure HTTP client, no session state
DsmSession (Scoped per-user)            — reads SID from ISession, wraps DsmApiClient
```

### Component Responsibilities

| Component | Lifetime | Responsibilities |
|-----------|----------|-----------------|
| **DsmSettingsService** | Singleton | Parse `/etc/synoinfo.conf`, return `Server`, `Port`, `Language`. Graceful fallback with defaults on failure. |
| **DsmApiClient** | Singleton | HTTP request execution, Form/JSON serialization, `IApiResponse` parsing, structured logging. Accepts SID as a parameter per call. Attaches cookie via `HttpRequestMessage`, not `DefaultRequestHeaders`. Caches `ApiInformations` via lazy initialization (fetched once, double-checked lock). |
| **DsmSession** | Scoped | Reads `DsmSid`/`DsmUsername` from `ISession`. Owns `_sessionValid`, `_lastSessionValidation` TTL cache. Owns `UserLanguage`, `UserDateFormat`, `UserTimeFormat`. Wraps `DsmApiClient` and injects SID into all calls. |

### How DsmSession Integrates with ISession

`DsmSession` is registered as **Scoped** and depends on `ISession` + `DsmApiClient`:

```csharp
sealed class DsmSession(DsmApiClient client, ISession session)
{
    string? Sid => session.GetString(ApplicationConstants.DsmSessionKey);
    string? Username => session.GetString(ApplicationConstants.DsmUsernameKey);

    // Per-user TTL cache (no more shared singleton state)
    bool _sessionValid;
    DateTime _lastSessionValidation = DateTime.MinValue;

    // Per-user preferences (no more shared singleton state)
    string? UserLanguage;
    string? UserDateFormat;
    string? UserTimeFormat;

    async Task<ApiResultData<R>> ExecuteAsync<R>(IApiParameters parameters)
        where R : IApiResponse
    {
        return await client.ExecuteAsync(Sid, parameters);
    }
}
```

`DsmApiClient.ExecuteAsync` accepts the SID as a parameter and attaches it
to the `HttpRequestMessage` via `Cookies` header — no mutation of
`DefaultRequestHeaders`, no shared state, no thread safety concerns.

---

## Issue #1: Singleton `DsmApiClient` with Mutable Session State

**SEVERITY:** Critical

**Problem:** `DsmApiClient` is a singleton with mutable per-session state
(`Sid`, `_sessionValid`, `_lastSessionValidation`, `UserLanguage`,
`UserDateFormat`, `UserTimeFormat`). If two users log in concurrently,
the second login overwrites the first user's SID in `DefaultRequestHeaders`.
All in-flight requests from the first user will carry the second user's session cookie.

**Impact:**

- Session leakage between concurrent users
- Race conditions on `DefaultRequestHeaders` (not thread-safe for concurrent writes)
- Non-atomic Remove/Add sequence in `SetSid()` can corrupt headers dictionary
- User preferences from one user can be read by another user
- TTL cache for session validation is shared across all users

**Resolution:** Extract all per-user state to `DsmSession` (Scoped). `DsmApiClient`
becomes a pure HTTP client with no session state. SID is passed as a parameter
to `ExecuteAsync<R>()` and attached per-request via `HttpRequestMessage`.

---

## Issue #2: `ReadSettings()` Throws Instead of Graceful Fallback

**SEVERITY:** High

**Problem:** `ReadSettings()` throws `FileNotFoundException` and
`MandatorySettingMissingException` at construction time. Since `SystemPreferences`
is initialized at construction, these exceptions crash the application at startup
with no recovery path.

**Impact:** Missing `/etc/synoinfo.conf` or missing required keys prevent the
application from starting entirely, rather than allowing degraded operation.

**Resolution:** Move `ReadSettings()` to `DsmSettingsService` (Singleton).
Wrap in try-catch, log warnings, and return sensible defaults. Decouple from API client lifecycle.

---

## Issue #3: `DsmApiClient` Mixes API Client and System Settings

**SEVERITY:** Medium

**Problem:** `DsmApiClient` owns both per-request API operations and static
system configuration. This violates single responsibility and creates a dependency
where the API client cannot function without system settings being available
at construction time.

**Impact:**

- Tight coupling between unrelated concerns
- System settings must be available before any API client can be instantiated
- Testing requires mocking both API behavior and file system access

**Resolution:** Extract to `DsmSettingsService`. `DsmApiClient` depends only on
`HttpClient`, `DsmSettingsService`, and `ILogger<ILogDsmApiClient>`.

---

## Current Consumers Inventory

| Consumer | Current Lifetime | Injects | Uses |
|----------|------------------|---------|------|
| `AuthenticationService` | Scoped | `DsmApiClient` | `ConnectAsync()`, `DisconnectAsync()`, `ValidateSessionAsync()`, `FetchUserLanguageAsync()`, `Sid`, user preferences |
| `FileSystemService` | **Singleton** | `DsmApiClient` | `ApiInformations`, `ExecuteAsync()` (authenticated calls) |
| `ReverseProxyManagerService` | **Singleton** | `DsmApiClient` | `ApiInformations`, `ExecuteAsync()`, `ExecuteSimpleAsync()` (authenticated calls) |
| `GlobalizationExtensions` | Startup (one-shot) | `DsmApiClient` (via `GetRequiredService`) | `SystemPreferences.Language` (no auth needed) |

**Lifetime mismatch:** `FileSystemService` and `ReverseProxyManagerService` are Singleton
but make authenticated API calls requiring a SID. After refactoring they'd need `DsmSession`
(Scoped), which is incompatible — Singleton cannot depend on Scoped. Both are stateless
per-request API wrappers and should be **changed to Scoped**.

**DI lifetime chain (after refactoring):**

```text
FileSystemService (Scoped) → DsmSession (Scoped) → DsmApiClient (Singleton) → HttpClient
ReverseProxyManagerService (Scoped) → DsmSession (Scoped) → DsmApiClient (Singleton) → HttpClient
```

Scoped → Singleton is valid in DI. `DsmApiClient` stays Singleton to preserve the
`HttpClient` instance and its connection pool. No performance impact.

---

## Implementation Plan

### Phase 1: Create `DsmSettingsService` ✅ COMPLETE

- Extract `ReadSettings()` and `GetMandatorySetting()` from `DsmApiClient`
- Wrap in try-catch with graceful fallback defaults (return `DsmSystemPreferences` with sensible values)
- Register as Singleton in DI container
- Consumers: `DsmApiClient` (server/port for URL construction),
  `GlobalizationExtensions` (system language — replace `DsmApiClient.SystemPreferences.Language`)
- Remove `SystemPreferences` property from `DsmApiClient`
- **Design decision:** Expose `Server`, `Port`, `Language` as direct properties (not `Preferences.Server`)
- **Design decision:** `DsmSettingsService` is DI-injected Singleton (not static) — enables testability
- **Logging:** `DsmSettingsServiceLoggingExtensions` with EventId 2800001–2800005

### Phase 2: Strip Session State from `DsmApiClient` ✅ COMPLETE

- Remove `Sid`, `_sessionValid`, `_lastSessionValidation` fields
- Remove `UserLanguage`, `UserDateFormat`, `UserTimeFormat` fields
- Remove `SetSid()`, `DisconnectAsync()`, `FetchUserLanguageAsync()`
- Remove `ConnectAsync()` — moved to `DsmSession` (Phase 3)
- Move `AuthenticateAsync()` to `DsmSession` (Phase 3)
- Keep `HandShakeAsync()` on `DsmApiClient` — drives lazy-init of `ApiInformations`
- `SystemPreferences` removed — `DsmSettingsService` injected (Phase 1)
- Accept SID as a parameter on `ExecuteAsync<R>()` and `ExecuteSimpleAsync()`
- Attach cookie via `HttpRequestMessage.Headers.Add()`, not `DefaultRequestHeaders`
- Keep: HTTP execution, serialization strategy, logging, `IApiResponse` parsing
- `ApiInformations`: kept on `DsmApiClient`, lazy-init with `SemaphoreLock` (Phase 2 deferral — remains TODO)
- **Consumers:** `FileSystemService` and `ReverseProxyManagerService` pass `null` for SID (temporary — Phase 6 fixes)
- **Consumers:** `AuthenticationService` stubbed (Phase 4 reimplements with `DsmSession`)

### Phase 3: Create `DsmSession` ✅ COMPLETE

- Depends on `DsmApiClient` + `ISession`
- Reads `DsmSid` and `DsmUsername` from `ISession` via `ApplicationConstants` keys
- Owns per-user `_sessionValid` / `_lastSessionValidation` TTL cache
- Owns per-user `UserLanguage`, `UserDateFormat`, `UserTimeFormat`
- Wraps `DsmApiClient` methods, injecting SID into all calls
- Implements login/logout/session validation flow (moves from `DsmApiClient`):
  - `ConnectAsync()` — authenticate + fetch user preferences (no handshake — `DsmApiClient` handles lazy-init of `ApiInformations` on first API call)
  - `DisconnectAsync()` — clear local state (SID cleared from `ISession` by caller)
  - `ValidateSessionAsync()` — per-user TTL cache, calls `SYNO.Core.User.get`
- **Placement:** `Ui/Services/DsmSession.cs` (not `Tools/`) — requires ASP.NET Core `ISession`
- **Logging:** `DsmSessionLoggingExtensions` with EventId 2900001–2900007
- **Consumers:** `FileSystemService` and `ReverseProxyManagerService` now depend on `DsmSession` instead of `DsmApiClient`; SID injected automatically from `ISession`

### Phase 4: Update `AuthenticationService` ✅ COMPLETE

- Depend on `DsmSession` instead of `DsmApiClient`
- Login: call `DsmSession.ConnectAsync()`, store `DsmSid`/`DsmUsername` in `ISession`,
  read preferences from `DsmSession`
- Logout: clear `ISession` keys
- Validation: call `DsmSession.ValidateSessionAsync()` (uses per-user TTL cache)
- Remove direct `DsmApiClient` constructor parameter

### Phase 5: Fix Consumer Lifetimes ✅ COMPLETE

- Change `FileSystemService` from Singleton to Scoped in `Program.cs`
- Change `ReverseProxyManagerService` from Singleton to Scoped in `Program.cs`
- Both are stateless per-request API wrappers — no reason for Singleton
- Register `DsmSession` as Scoped in `Program.cs`

### Phase 6: Update Remaining Consumers ✅ COMPLETE

- `FileSystemService`: depend on `DsmSession` instead of `DsmApiClient`
- `ReverseProxyManagerService`: depend on `DsmSession` instead of `DsmApiClient`
- `GlobalizationExtensions`: depend on `DsmSettingsService` instead of `DsmApiClient`
- Verify all callers receive correct session state per request scope

### Phase 8: Decouple `ApiInformationCollection` from Parameters and `DsmSession` ✅ COMPLETE

- **Goal:** `DsmApiClient` owns the entire `ApiInformations` lifecycle — lazy-init, locking, lookup.
  Parameters become pure serialization shells. `DsmSession` is decoupled from `ApiInformationCollection`
  entirely — it no longer knows about API metadata, handshake, or collection state.
- **`IApiParameters.BuildUrl`** signature change: `BuildUrl(string server, int port, string path)` — accepts resolved path as parameter
- **`ApiParametersBase<T>` constructor** — remove `ApiInformationCollection` parameter; no longer does `informations.Get(Name)` lookup
- **`ApiParametersBase<T>`** — remove `Path` property, version validation, `CreateDefaultHandshakeInfo()`, `NullReferenceException("Empty API Information.")` anti-pattern
- **`DsmApiClient.ExecuteAsync`** — does lookup before calling `BuildUrl`:

  ```csharp
  var apiInfo = ApiInformations.Get(parameters.Name)
      ?? throw new InvalidOperationException($"Unknown API: {parameters.Name}");
  var url = parameters.BuildUrl(settingsService.Server, settingsService.Port, apiInfo.Path);
  ```

- **`InformationsQueryParameters`** — special-cased in `DsmApiClient.ExecuteAsync` (handshake API doesn't need lookup):

  ```csharp
  var path = (parameters.Name == ApiConstants.Info)
      ? ApiConstants.Handshake
      : apiInfo.Path;
  ```

- **`DsmApiClient` lazy-init** — private `EnsureInitializedAsync()` with `SemaphoreLock` double-checked locking;
  called from `ExecuteAsync` before any API call; eliminates per-session handshake

- **`DsmSession` cleanup** (decouple from `ApiInformationCollection`):

  - Remove `ApiInformations` proxy property (`public ApiInformationCollection ApiInformations => _client.ApiInformations;`)
  - Remove `HandShakeAsync()` — moved to `DsmApiClient.EnsureInitializedAsync()`
  - Remove handshake guard in `AuthenticateAsync` (`if _client.ApiInformations.Get(Auth) is null`)
  - Remove `_client.ApiInformations.Replace(result.Data)` — `DsmApiClient` handles it internally
  - Remove `_client.ApiInformations` from all parameter constructors:
    - `ValidateSessionAsync`: `new CoreUserGetParameters(Username)` instead of `new CoreUserGetParameters(_client.ApiInformations, ...)`
    - `AuthenticateAsync`: `new AuthLoginParameters(login)` instead of `new AuthLoginParameters(_client.ApiInformations, login)`
    - `HandShakeAsync`: `new InformationsQueryParameters()` instead of `new InformationsQueryParameters(_client.ApiInformations)` (method removed anyway)
    - `FetchUserPreferencesAsync`: `new CoreUserSettingsParameters()` instead of `new CoreUserSettingsParameters(_client.ApiInformations)`
  - After cleanup, `DsmSession` only knows about `DsmApiClient.ExecuteAsync()` and `ISession` — zero API metadata awareness

- **Parameter classes** — all constructors simplified: `new AuthLoginParameters(login)` instead of `new AuthLoginParameters(informations, login)`
- **Files impacted:**
  - `Data/DsmApi/Parameters/IApiParameters.cs` — `BuildUrl` signature
  - `Data/DsmApi/Parameters/ApiParametersBase.cs` — remove collection, Path, version validation
  - `Data/DsmApi/Parameters/Info/InformationsQueryParameters.cs` — remove collection from constructor
  - All parameter classes (~20 files) — remove `ApiInformationCollection` from constructor
  - `Tools/Network/DsmApiClient.cs` — add `ISemaphoreOwner`, lazy-init `EnsureInitializedAsync()`, lookup in `ExecuteAsync`
  - `Ui/Services/DsmSession.cs` — remove `HandShakeAsync`, `ApiInformations` proxy, handshake guard, all `_client.ApiInformations` usages
  - `Ui/Services/FileSystemService.cs` — remove `dsmSession.ApiInformations` from parameter constructors
  - `Ui/Services/ReverseProxyManagerService.cs` — same

### Phase 7: Tests

**`DsmSettingsService` tests:**

- `ReadSettings_FileExists_ReturnsPreferences` — valid config file returns correct `Server`, `Port`, `Language`
- `ReadSettings_FileMissing_ReturnsDefaults` — missing `/etc/synoinfo.conf` returns defaults, no exception
- `ReadSettings_MissingKey_ReturnsDefaults` — missing `external_host_ip` returns default server/port, no exception
- `ReadSettings_MalformedFile_ReturnsDefaults` — file with no `=` lines returns defaults

**`DsmApiClient` tests (stateless, Singleton):**

- `ExecuteAsync_AttachesCookieHeader` — SID passed as parameter appears in request `Cookie` header
- `ExecuteAsync_NoSid_SkipsCookieHeader` — null SID omits cookie header
- `ExecuteAsync_FormSerialization_UsesToForm` — `SerializationFormats.Form` calls `ToForm()`
- `ExecuteAsync_JsonSerialization_UsesToJson` — `SerializationFormats.Json` calls `ToJson()`
- `ApiInformations_LazyInit_FetchedOnce` — first call triggers handshake, subsequent calls return cache
- `ApiInformations_ConcurrentFirstCall_FetchedOnce` — 10 concurrent callers, only 1 handshake executes
  (use `SemaphoreLock` pattern from `SemaphoreLockTests.cs` as reference)
- `ExecuteAsync_HttpError_ReturnsDefault` — non-200 response returns `null`
- `ExecuteAsync_UsesDsmSettingsService` — URL built from `DsmSettingsService.Server`/`Port`

**Mocking strategy:** use `HttpMessageHandler` mock (via `HttpMessageHandlerStub` or `DelegatingHandler`)
to intercept HTTP calls. No need to mock `HttpClient` directly — mock the transport layer.

**`DsmSession` tests:**

- `ConnectAsync_Success_StoresPreferences` — successful login sets `UserLanguage`, `UserDateFormat`, `UserTimeFormat`
- `ConnectAsync_Failure_ReturnsFalse` — auth failure returns `false`
- `ValidateSessionAsync_CachedWithinTtl_SkipsApiCall` — second call within 1 minute skips API
- `ValidateSessionAsync_ExpiredTtl_CallsApi` — call after TTL triggers `SYNO.Core.User.get`
- `ValidateSessionAsync_InvalidSid_ReturnsFalse` — auth error (-4) returns `false`
- `DisconnectAsync_ClearsState` — clears preferences and validation cache
- `ExecuteAsync_SidFromSession_InjectedInCall` — SID read from mock `ISession` is passed to client

**Mocking strategy:** mock `ISession` via `Mock<ISession>` (setup `GetString` key lookups),
mock `DsmApiClient` via `Mock<DsmApiClient>` or use `HttpMessageHandler` stub.

**Consumer regression tests:**

- `FileSystemService_GetSharedFoldersAsync_UsesSessionSid` — verify `DsmSession` is used (not `DsmApiClient`)
- `ReverseProxyManagerService_CreateAsync_UsesSessionSid` — same verification
- Follow existing patterns from `SiteLifecycleManagerTests.cs` (fake implementations, Moq mocks)

---

## Issue Resolution Mapping

| Issue | Phase | Fix |
|-------|-------|-----|
| **#1 Thread Safety on `DefaultRequestHeaders`** | Phase 2 | Removes `SetSid()` and all `DefaultRequestHeaders` mutations. Cookie attached per-request via `HttpRequestMessage.Headers.Add()` — no shared mutable dictionary, no concurrent writes. |
| **#2 `ReadSettings()` Throws** | Phase 1 | Extracted to `DsmSettingsService` with try-catch and default fallbacks. Missing file or keys no longer crash startup. |
| **#3 Mixed Responsibilities** | Phase 1 + 2 | `ReadSettings()` → `DsmSettingsService`. Session state → `DsmSession`. `DsmApiClient` is pure HTTP client. |
| **#9 Singleton Mutable Session State** | Phase 2 + 3 | `Sid`, `_sessionValid`, `_lastSessionValidation`, user preferences removed from `DsmApiClient`. Moved to `DsmSession` (Scoped, per-user, reads `ISession`). |
| **`ApiInformations` concurrent mutation** | Phase 8 | Lazy initialization with double-checked lock via `SemaphoreLock` in `DsmApiClient`. Fetched once, cached forever. Parameters decoupled — `BuildUrl` accepts resolved path. |
| **Consumer lifetime mismatch** | Phase 5 + 6 | `FileSystemService` and `ReverseProxyManagerService` changed from Singleton to Scoped. Compatible with Scoped `DsmSession`. |

---

## Reference

### File Paths

| Component | Path |
|-----------|------|
| `DsmApiClient` | `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs` |
| `DsmSettingsService` (new) | `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/DsmSettingsService.cs` |
| `DsmSession` (new) | `src/Askyl.Dsm.WebHosting.Tools/Network/DsmSession.cs` |
| `DsmSystemPreferences` | `src/Askyl.Dsm.WebHosting.Data/Domain/DsmSystem/DsmSystemPreferences.cs` |
| `ApiInformationCollection` | `src/Askyl.Dsm.WebHosting.Data/DsmApi/Models/Core/ApiInformationCollection.cs` |
| `ApiParametersBase` | `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/ApiParametersBase.cs` |
| `IApiParameters` | `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/IApiParameters.cs` |
| `ApiResponseBase` + `IApiResponse` | `src/Askyl.Dsm.WebHosting.Data/DsmApi/Responses/ApiResponseBase.cs` |
| `SemaphoreLock` | `src/Askyl.Dsm.WebHosting.Tools/Threading/SemaphoreLock.cs` |
| `AuthenticationService` | `src/Askyl.Dsm.WebHosting.Ui/Services/AuthenticationService.cs` |
| `FileSystemService` | `src/Askyl.Dsm.WebHosting.Ui/Services/FileSystemService.cs` |
| `ReverseProxyManagerService` | `src/Askyl.Dsm.WebHosting.Ui/Services/ReverseProxyManagerService.cs` |
| `GlobalizationExtensions` | `src/Askyl.Dsm.WebHosting.Ui/Extensions/GlobalizationExtensions.cs` |
| `ApplicationConstants` | `src/Askyl.Dsm.WebHosting.Constants/Application/ApplicationConstants.cs` |
| `Program.cs` (DI registration) | `src/Askyl.Dsm.WebHosting.Ui/Program.cs` |
| `DsmApiLoggingExtensions` | `src/Askyl.Dsm.WebHosting.Logging/Server/DsmApi/DsmApiLoggingExtensions.cs` |
| `LogEventIds` | `src/Askyl.Dsm.WebHosting.Constants/Logging/LogEventIds.cs` |

### `DsmSystemPreferences` Record

```csharp
// src/Askyl.Dsm.WebHosting.Data/Domain/DsmSystem/DsmSystemPreferences.cs
public sealed class DsmSystemPreferences(
    string server,
    int port,
    string language)
{
    public string Server { get; init; } = server;
    public int Port { get; init; } = port;
    public string Language { get; init; } = language;
}
```

### `ApiInformationCollection` API

```csharp
// src/Askyl.Dsm.WebHosting.Data/DsmApi/Models/Core/ApiInformationCollection.cs
public class ApiInformationCollection
{
    private ConcurrentDictionary<string, ApiInformation>? _collection;

    public void Replace(IDictionary<string, ApiInformation> source);
    public ApiInformation? Get(string name);
}
```

### `SemaphoreLock` Usage

```csharp
// src/Askyl.Dsm.WebHosting.Tools/Threading/SemaphoreLock.cs
// Requires ISemaphoreOwner:
public interface ISemaphoreOwner
{
    SemaphoreSlim Semaphore { get; }
}

// Usage:
using var @lock = await SemaphoreLock.AcquireAsync(owner);
// ... critical section ...
// Automatically released on Dispose
```

### `IApiParameters` Interface

```csharp
// src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/IApiParameters.cs
public interface IApiParameters
{
    string Name { get; }
    string Path { get; }
    int Version { get; }
    string Method { get; }
    SerializationFormats SerializationFormat { get; }
    string BuildUrl(string server, int port);
    StringContent ToForm();
    StringContent ToJson();
}
```

### `IApiResponse` Interface

```csharp
// src/Askyl.Dsm.WebHosting.Data/DsmApi/Responses/ApiResponseBase.cs
public interface IApiResponse
{
    bool Success { get; }
    ApiError? Error { get; }
}

public class ApiResponseBase<T> : IApiResponse where T : class, new()
{
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public bool Success { get; set; }
}
```

### Session Constants

```csharp
// src/Askyl.Dsm.WebHosting.Constants/Application/ApplicationConstants.cs
public const string DsmSessionKey = "DsmSid";
public const string DsmUsernameKey = "DsmUsername";
public const int SessionValidationTtlMinutes = 1;
public const int SessionTimeoutMinutes = 30;
public const string HttpClientName = "UiClient";
```

### `ISession` Extension Methods (ASP.NET Core)

```csharp
// Microsoft.AspNetCore.Http.ISession extensions
string? session.GetString(string key);
void session.SetString(string key, string value);
void session.Remove(string key);
```

### Current DI Registration (`Program.cs`)

```csharp
// Line 59 — replace during refactoring
builder.Services.AddSingleton<DsmApiClient>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();  // → Scoped
builder.Services.AddSingleton<IReverseProxyManagerService, ReverseProxyManagerService>();  // → Scoped
```

### Current `DsmApiClient` Constructor

```csharp
public class DsmApiClient(IHttpClientFactory httpClientFactory, ILogger<ILogDsmApiClient> logger)
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ApplicationConstants.HttpClientName);
    // Fields to remove: Sid, _sessionValid, _lastSessionValidation,
    //   UserLanguage, UserDateFormat, UserTimeFormat, SystemPreferences, ApiInformations
}
```

### Logging Extensions (`ILogDsmApiClient`)

Existing `[LoggerMessage]` methods in `DsmApiLoggingExtensions.cs`:

| Method | EventId | Format |
|--------|---------|--------|
| `ConfigurationFileNotFound` | 2000001 | `"Configuration file not found: {ConfigurationFileName}"` |
| `ConfigurationLoaded` | 2000002 | `"Configuration loaded with {Count} settings"` |
| `MandatorySettingMissing` | 2000003 | `"Mandatory setting missing: {SettingKey}"` |
| `ApiRequest` | 2000004 | `"API {Method} {Url} → {StatusCode} ({Duration}ms)"` |
| `AuthenticationFailed` | 2000005 | `"Authentication failed: {ErrorMessage}"` |
| `ApiError` | 2000006 | `"API error: {ErrorMessage} (code: {ErrorCode})"` |
| `Connecting` | 2000007 | `"Connecting to {Server}:{Port}"` |
| `Connected` | 2000008 | `"Connected to DSM"` |
| `Disconnecting` | 2000009 | `"Disconnecting from DSM"` |
| `Disconnected` | 2000010 | `"Disconnected from DSM"` |
| `HandshakeSuccess` | 2000011 | `"Handshake successful"` |
| `HandshakeFailure` | 2000012 | `"Handshake failed"` |
| `FetchUserPreferencesFailed` | 2000013 | `"Failed to fetch user preferences: {Error}"` |

**EventId range:** 2000001–2000013 (base 2000000, see `LogEventIds.cs`).

**New EventId ranges for refactored components:**

| Component | Base | Range | Region |
|-----------|------|-------|--------|
| `DsmSettingsService` | 2800000 | 2800001–2800006 | Infrastructure |
| `DsmSession` | 2900000 | 2900001–2900010 | Session |

Existing methods to migrate:

- `ConfigurationFileNotFound`, `ConfigurationLoaded`, `MandatorySettingMissing` → `DsmSettingsService` (2800001–2800003)
- `Connecting`, `Connected`, `Disconnecting`, `Disconnected`, `HandshakeSuccess`, `HandshakeFailure`, `FetchUserPreferencesFailed` → `DsmSession` (2900001–2900007)
- `AuthenticationFailed` → stays on `DsmApiClient` (auth is HTTP-layer concern)

### `ApiParametersBase` Constructor Behavior (After Phase 8)

```csharp
// All parameter classes extend ApiParametersBase<T>
// Constructor takes only the entry payload — no ApiInformationCollection
protected ApiParametersBase(T? entry = null)
{
    Parameters = entry ?? new();
}

// BuildUrl accepts resolved path from DsmApiClient
public string BuildUrl(string server, int port, string path)
    => $"https://{server}:{port}/webapi/{path}/{Name}";
```

`DsmApiClient` owns the full `ApiInformations` lifecycle: lazy-init with `SemaphoreLock`, lookup, and path resolution.
Parameters are pure serialization shells with zero API metadata awareness.

### Test Patterns (Existing)

- **Framework:** xUnit + Moq + FluentAssertions
- **Style:** Arrange/Act/Assert, `[Fact]` attributes
- **Fake implementations:** `FakeProcessRunner`, `FakeProcessHandle` (see `SiteLifecycleManagerTests.cs`)
- **Semaphore tests:** `TestSemaphoreOwner` implements `ISemaphoreOwner` (see `SemaphoreLockTests.cs`)
- **Project:** `src/Askyl.Dsm.WebHosting.Tests/`

---

## Notes

- Issues identified during globalization branch code review (2026-06-14)
- Refactoring direction agreed 2026-06-21: split into three components
- `ISession` is already the correct per-user store; `DsmSession` bridges it to `DsmApiClient`
- `ApiInformations` fetched once via lazy init (Option B, double-checked lock)
- `ApiInformations` behavioral change (fetch once forever) is safe: DSM cannot change API metadata without a system restart, which restarts the application
- `HandShakeAsync()` stays on `DsmApiClient` to drive lazy-init of `ApiInformations`; `DsmSession.ConnectAsync()` only calls `AuthenticateAsync()` + `FetchUserLanguageAsync()`
- `FileSystemService` and `ReverseProxyManagerService` must change from Singleton to Scoped
- EventId ranges: `DsmSettingsService` (2800001–2800005), `DsmSession` (2900001–2900010)
- **Phase 1 complete (2026-06-22):** `DsmSettingsService` extracts settings from `DsmApiClient`; graceful fallback defaults; direct property mapping (`Server`, `Port`, `Language`)
- **Phase 2 complete (2026-06-22):** `DsmApiClient` is now a pure HTTP client with no session state; SID passed as parameter per-call; cookie attached per-request via `HttpRequestMessage`
- **Phase 3 complete (2026-06-22):** `DsmSession` created in `Ui/Services/` (requires ASP.NET Core `ISession`); owns per-user TTL cache, preferences, and login/logout/validation flow
- **Phase 4 complete (2026-06-22):** `AuthenticationService` depends on `DsmSession`; simplified login/logout/validation flow
- **Phase 5 complete (2026-06-22):** `FileSystemService` and `ReverseProxyManagerService` changed from Singleton to Scoped; `WebSiteHostingService` uses `IServiceScopeFactory` to resolve scoped dependencies
- **Phase 6 complete (2026-06-22):** All consumers updated; `GlobalizationExtensions` depends on `DsmSettingsService`; DI chain valid: `Scoped → Scoped → Singleton → HttpClient`
- **Phase 7 complete (2026-06-22):** 18 tests across 3 test files:
  - `DsmSettingsServiceTests` (5): service construction, config parsing, mandatory settings
  - `DsmApiClientTests` (8): cookie attachment, serialization, HTTP errors, URL, concurrency
  - `DsmSessionTests` (5): session validation, disconnect, SID delegation
  - Consumer regression tests not feasible — `DsmSession` and `DsmSettingsService` are `sealed`
- **Phase 8 complete (2026-06-23):** `ApiInformationCollection` fully decoupled from parameters and `DsmSession`;
  `DsmApiClient` owns lifecycle with `SemaphoreLock` lazy-init; `BuildUrl` accepts resolved path;
  parameters are pure serialization shells; `DsmSession` has zero API metadata awareness

## Future Work

- **Unseal `DsmSession` and `DsmSettingsService`:** Both are `sealed`, preventing mocking. Defer until dedicated testability pass.

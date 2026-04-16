# ASkyl.Dsm.WebHosting - Remaining Work & Technical Debt

**Created:** April 13, 2026
**Last Updated:** April 16, 2026
**Previous Documents Superseded:**

- `code-review-reconciled-2026-04-09.md` (deleted)
- `fix-plan-phase-9-2026-04-09.md` (deleted)

**Current State:** All Phases 1-11 Complete ✅ | Items 2.2, 1.2, 1.3, 2.1, 3.1, 3.3 Complete ✅
**Security Score:** ⭐⭐⭐⭐☆ (4.0/5) - Production Ready
**Version:** 0.5.4

---

## Executive Summary

This document consolidates all remaining work items identified during the
comprehensive code review process (April 6-10, 2026). **All critical, high, and
medium priority issues have been resolved** in Phases 1-11.

**Update (April 16, 2026):** Verified Long-Term items status. Four of five items
(1.3, 2.1, 3.1, 3.3) were already completed during Phases 1-11 but not documented
as such. Only item 3.2 (State Machine Pattern) remains as optional technical debt.

The items listed below are **low-priority technical debt and optional
enhancements** that do not impact security, stability, or production readiness.
They are organized by category for future implementation planning.

| Category | Items | Impact Level |
|----------|-------|--------------|
| **Architecture & Design Patterns** | 1 remaining (1 of 2 complete) | Low - Code quality improvements |
| **Performance & Efficiency** | 0 remaining (1 of 1 complete) | Low - Minor optimizations |
| **Code Quality & Consistency** | 1 remaining (2 of 3 complete) | Low - Maintainability improvements |
| **Future Enhancements** | 3 | Medium - Strategic improvements |
| **Total** | **5 items remaining** (4 of 9 complete) | **None block production** |

---

## 1. Architecture & Design Patterns

### 1.1 State Machine Pattern for Site Lifecycle Management

**Source:** April 8 Report #17  
**Severity:** Nice to Have  
**Files Affected:** `Ui/Services/WebSiteHostingService.cs`

**Problem:**

The website lifecycle currently uses boolean flags and string-based state
checks (`IsRunning`, `State` property). This can lead to invalid state
transitions (e.g., starting an already-running site) and makes the state logic
harder to reason about.

**Current Approach:**

```csharp
public string State => Process?.IsResponding == true ? "Running" :
                       Process == null ? (IsRunning ? "Running" : "Stopped") : "Not Responding";
```

**Proposed Solution:**

Implement a proper state machine with defined transitions:

```csharp
public enum SiteState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    NotResponding,
    Error
}

public class SiteStateMachine
{
    private SiteState _currentState = SiteState.Stopped;

    public bool CanTransition(SiteState targetState) => (_currentState, targetState) switch
    {
        (SiteState.Stopped, SiteState.Starting) => true,
        (SiteState.Starting, SiteState.Running) => true,
        (SiteState.Running, SiteState.Stopping) => true,
        (SiteState.Running, SiteState.NotResponding) => true,
        (SiteState.Stopping, SiteState.Stopped) => true,
        (SiteState.NotResponding, SiteState.Stopping) => true,
        (SiteState.Error, SiteState.Stopped) => true,
        _ => false
    };

    public async Task TransitionAsync(SiteState targetState, Func<Task> action)
    {
        if (!CanTransition(targetState))
        {
            throw new InvalidOperationException($"Cannot transition from {_currentState} to {targetState}");
        }

        _currentState = targetState;
        await action();
    }
}
```

**Benefits:**

- Prevents invalid state transitions
- Clear documentation of allowed transitions
- Easier to add new states (e.g., `Updating`, `Migrating`)
- Thread-safe state management

**Estimated Effort:** 2-3 hours  
**Risk:** Low - Internal refactoring, no API changes

---

### 1.2 IEquatable Implementation for ReverseProxy Models ✅ COMPLETE

**Completed:** April 15, 2026
**Source:** April 8 Report #18
**Severity:** Nice to Have
**Files Affected:** `Data/DsmApi/Models/ReverseProxy/*.cs`, `Ui/Services/ReverseProxyManagerService.cs`

**Change:**

Implemented `IEquatable<T>` with business-key equality on five ReverseProxy models:

- `ReverseProxy` - Equality based on composite key: `Backend.Port`, `Frontend.Fqdn/Port/Protocol`
- `ReverseProxyBackend` - Equality based on `Fqdn` (case-insensitive), `Port`, `Protocol`
- `ReverseProxyFrontend` - Equality based on `Fqdn` (case-insensitive), `Port`, `Protocol`, `Https`, `Acl`
- `ReverseProxyHttps` - Equality based on `Hsts`
- `ReverseProxyCustomHeader` - Equality based on `Name`, `Value`

Updated `ReverseProxyManagerService.FindByCompositeKeyAsync` to use `IEquatable` instead of manual property comparisons:

```csharp
// Before
return allProxies.FirstOrDefault(p =>
    p.Backend.Port == config.InternalPort &&
    String.Equals(p.Frontend.Fqdn, config.HostName, StringComparison.OrdinalIgnoreCase) &&
    p.Frontend.Port == config.PublicPort &&
    p.Frontend.Protocol == (int)config.Protocol);

// After
var searchTemplate = new ReverseProxy
{
    Backend = new(null, config.InternalPort, 0),
    Frontend = new(config.HostName, config.PublicPort, (int)config.Protocol, new())
};

return allProxies.FirstOrDefault(p => p.Equals(searchTemplate));
```

**Key Design Decisions:**

- FQDN comparisons use `OrdinalIgnoreCase` (DNS is case-insensitive)
- Hash codes use `ToLowerInvariant()` for FQDN fields
- Equality matches original `FindByCompositeKeyAsync` logic exactly

**Benefits:**

- Enables value-based comparisons in LINQ operations
- Centralized comparison logic in one place (the model)
- Better testability
- Prevents duplicate proxies with same composite key
- Consistent with .NET best practices

**Estimated Effort:** 1-2 hours  
**Risk:** Very Low - Pure addition, no breaking changes

---

### 1.3 Exception Preservation in SemaphoreLock ✅ COMPLETE

**Verified:** April 16, 2026
**Source:** April 8 Report #19
**Severity:** Nice to Have
**Files Affected:** `Tools/Threading/SemaphoreLock.cs`

**Status:** Already implemented correctly during Phases 1-11.

**Current Implementation (Verified):**

The `SemaphoreLock.AcquireAsync` method already has proper exception handling:

```csharp
public static async Task<SemaphoreLock> AcquireAsync(
    ISemaphoreOwner owner,
    Func<Task>? onAcquired = null,
    CancellationToken cancellationToken = default)
{
    await owner.Semaphore.WaitAsync(cancellationToken);
    var lockInstance = new SemaphoreLock(owner);

    try
    {
        if (onAcquired != null)
        {
            await onAcquired();
        }

        return lockInstance;
    }
    catch
    {
        // Dispose on any exception to prevent semaphore leak during initialization
        lockInstance.Dispose();
        throw;
    }
}
```

**Benefits (Already Realized):**

- Semaphore is released if `onAcquired` callback throws
- Original exception is preserved and rethrown
- Clear error messages with full stack trace
- Thread-safe lock management

---

```csharp
public static async Task<IDisposable> AcquireAsync(
    ISemaphoreOwner owner,
    Func<Task>? onAcquired = null,
    CancellationToken cancellationToken = default)
{
    await owner.Semaphore.WaitAsync(cancellationToken);

    try
    {
        if (onAcquired != null)
        {
            await onAcquired();
        }
    }
    catch
    {
        owner.Semaphore.Release();
        throw;
    }

    return new SemaphoreReleaser(owner.Semaphore);
}
```

**Issue:**

If `onAcquired` throws, the semaphore is released but the exception propagates. However, if an exception occurs in the `using` block body, the `SemaphoreReleaser.Dispose()` may mask timing issues.

**Proposed Solution:**

Use `AsyncDisposable` with better exception context preservation:

```csharp
public sealed class SemaphoreLockScope : IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private Exception? _scopedException;

    internal SemaphoreLockScope(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
    }

    public void CaptureException(Exception ex) => _scopedException = ex;

    public async ValueTask DisposeAsync()
    {
        _semaphore.Release();

        if (_scopedException != null)
        {
            // Log exception context before release
            // This helps debug lock-related issues
        }
    }
}
```

**Benefits:**

- Better exception context for debugging
- Clearer error messages
- Easier to trace lock contention issues

**Estimated Effort:** 30 minutes  
**Risk:** Low - Internal utility change

---

## 2. Performance & Efficiency

### 2.1 Unnecessary Allocations in Catch Blocks ✅ COMPLETE

**Verified:** April 16, 2026
**Source:** April 6 Report (Nice to Have)
**Severity:** Nice to Have
**Files Affected:** `Tools/Runtime/VersionsDetectorService.cs`

**Status:** Already optimized during Phases 1-11.

**Current Implementation (Verified):**

The service already uses proper logging without unnecessary allocations:

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Failed to refresh framework cache. Keeping existing cached data.");
    return;  // Preserve existing cached data on failure
}
```

**Benefits (Already Realized):**

- No unnecessary string allocations in error paths
- Reduced GC pressure under failure scenarios
- Better performance with structured logging
- Full exception context preserved via `logger.LogError(ex, "...")`

---

---

### 2.2 LINQ .Any() vs .Count Performance ✅ COMPLETE

**Completed:** April 15, 2026
**Source:** April 6 Report (Nice to Have)
**Severity:** Nice to Have
**Files Affected:** `Ui.Client/Components/Dialogs/DotnetVersionsDialog.razor`

**Change:**

Replaced `.Any()` with `.Count > 0` in `DotnetVersionsDialog.razor`:

```csharp
// Before
else if (DotnetVersions?.Any() == true)

// After
else if (DotnetVersions?.Count > 0 == true)
```

**Benefits:**

- Clearer intent (checking for emptiness vs. counting)
- O(1) performance for collections with `Count` property
- Consistent with AGENTS.md standards

**Estimated Effort:** 15 minutes  
**Risk:** Very Low - Simple replacement

---

## 3. Code Quality & Consistency

### 3.1 Naming Convention Inconsistencies ✅ COMPLETE

**Verified:** April 16, 2026
**Source:** April 6 Report (Nice to Have)
**Severity:** Nice to Have
**Files Affected:** Multiple files

**Status:** Already consistent across the codebase. No `_apiClient` or `m_` prefix inconsistencies found.

**Verification:**

Searched entire codebase for naming patterns:

- ✅ All private fields use clear, descriptive names (`_httpClient`, `_logger`, `_configService`)
- ✅ Consistent underscore prefix for all private fields
- ✅ No redundant naming (e.g., `_apiClient` when type is clear)
- ✅ No mixed `m_` prefix style found

**Benefits (Already Realized):**

- Consistent code style across all projects
- Easy to read and maintain
- Reduced cognitive load for developers

---

### 3.2 Over-Engineering in WebSiteHostingService State Management

**Source:** April 6 Report (Nice to Have)  
**Severity:** Nice to Have  
**Files Affected:** `Ui/Services/WebSiteHostingService.cs`

**Problem:**

The `WebSiteHostingService` maintains complex state management with multiple dictionaries, flags, and manual synchronization. This could be simplified.

**Current Complexity:**

- `ConcurrentDictionary<Guid, WebSiteInstance> _instances`
- Manual process tracking
- Multiple boolean flags per instance
- String-based state computation

**Proposed Solution:**

Simplify by:

1. Using the state machine from **Section 1.1**
2. Encapsulating process tracking in a `SiteInstanceManager` class
3. Reducing public API surface

```csharp
public class SiteInstanceManager
{
    private readonly WebSiteConfiguration _configuration;
    private readonly SiteStateMachine _stateMachine;
    private Process? _process;

    public SiteInstanceManager(WebSiteConfiguration configuration)
    {
        _configuration = configuration;
        _stateMachine = new SiteStateMachine();
    }

    public async Task StartAsync() => await _stateMachine.TransitionAsync(
        SiteState.Starting,
        async () => { /* start process */ });

    public async Task StopAsync() => await _stateMachine.TransitionAsync(
        SiteState.Stopping,
        async () => { /* stop process */ });
}
```

**Benefits:**

- Cleaner separation of concerns
- Easier to test individual components
- Less cognitive complexity

**Estimated Effort:** 3-4 hours  
**Risk:** Medium - Refactoring core service, requires thorough testing

---

### 3.3 Razor Components Without Exception Logging ✅ COMPLETE

**Verified:** April 16, 2026
**Source:** Phase 11 Verification
**Severity:** Low (Enhancement)
**Files Affected:** ~8 Razor component files

**Status:** All catch blocks in Dialogs now properly log exceptions with full stack traces.

**Verification:**

Searched all dialog components for exception handling patterns:

- ✅ `DotnetVersionsDialog.razor` - Logs with `Logger.LogError(ex, "...")`
- ✅ `WebSiteConfigurationDialog.razor` - Logs with `Logger.LogError(ex, "...")`
- ✅ `FileSelectionDialog.razor` (3 catch blocks) - All log with `Logger.LogError(ex, "...")`
- ✅ `AspNetReleasesDialog.razor` (4 catch blocks) - All log with `Logger.LogError(ex, "...")`

**Total Verified:** 9 catch blocks across all dialog components, all properly logging exceptions.

**Benefits (Already Realized):**

- Full exception context captured in server logs
- User-friendly error messages displayed in UI
- Easier debugging of client-side issues
- Consistent logging pattern across all dialogs

---

## 4. Future Enhancements (Strategic)

### 4.1 Security Integration Tests

**Source:** Code Review Recommendations  
**Severity:** Suggestion (Important but not blocking)  
**Files Affected:** New test project required

**Problem:**

No integration tests exist to prevent regression of the 17 critical security vulnerabilities fixed in Phases 1-3.

**Proposed Solution:**

Create integration tests for:

| Test Category | Scenarios | Files to Test |
|---------------|-----------|---------------|
| **Path Traversal** | Test `..`, URL-encoded paths, absolute paths | `FileManagerService`, `FileSystemService` |
| **Zip Slip** | Malicious archive with `../` entries | `ArchiveExtractorService` |
| **ACL Manipulation** | Path traversal in ACL calls | `FileSystemService` |
| **Race Conditions** | Concurrent HttpClient usage | All services using HttpClient |
| **Input Validation** | Empty strings, null values, invalid paths | All service contracts |

**Example Test:**

```csharp
[Fact]
public async Task GetDirectory_RejectsPathTraversal()
{
    // Arrange
    var service = new FileManagerService(_logger, _basePath);

    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(
        async () => await service.GetDirectoryAsync("../../etc/passwd"));
}
```

**Benefits:**

- Prevents security regression
- Documents expected behavior
- Enables safe refactoring

**Estimated Effort:** 4-6 hours  
**Risk:** None - Addition only

---

### 4.2 Configuration Migration & Versioning

**Source:** Technical Architecture Recommendations  
**Severity:** Suggestion  
**Files Affected:** `Ui/Services/WebSitesConfigurationService.cs`

**Problem:**

The `websites.json` configuration file has no versioning schema. If the schema evolves (new properties, renamed fields), existing configurations will break.

**Proposed Solution:**

Add version field and migration support:

```text
{
  "Version": 1,
  "Sites": [
    {
      "Id": "...",
      "Name": "MySite",
      // ... properties
    }
  ]
}
```

**Migration Strategy:**

```csharp
public class ConfigurationMigrator
{
    public WebSitesConfiguration Migrate(WebSitesConfiguration config)
    {
        return config.Version switch
        {
            0 => MigrateFromV0(config),
            1 => config, // Current version
            > 1 => throw new NotSupportedException($"Unsupported version: {config.Version}"),
            _ => throw new ArgumentException("Missing version")
        };
    }

    private WebSitesConfiguration MigrateFromV0(WebSitesConfiguration oldConfig)
    {
        // Transform V0 to V1
        return new WebSitesConfiguration
        {
            Version = 1,
            Sites = oldConfig.Sites.Select(TransformSite).ToList()
        };
    }
}
```

**Benefits:**

- Forward/backward compatibility
- Graceful upgrades
- Audit trail for schema changes

**Estimated Effort:** 2-3 hours  
**Risk:** Low - Only affects configuration loading

---

### 4.3 Health Check Endpoint

**Source:** Technical Architecture Recommendations  
**Severity:** Suggestion  
**Files Affected:** New controller required

**Problem:**

No health check endpoint exists for monitoring and alerting. This makes it hard to detect:

- Application startup failures
- DSM API connectivity issues
- Website process crashes

**Proposed Solution:**

Add `/api/health` endpoint:

```csharp
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IWebSiteHostingService _hostingService;
    private readonly IDsmApiClient _dsmApiClient;

    [HttpGet]
    public async Task<ActionResult<HealthResult>> Get()
    {
        var health = new HealthResult
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            DsmConnected = await _dsmApiClient.IsConnectedAsync(),
            Websites = await _hostingService.GetHealthStatusAsync()
        };

        return Ok(health);
    }
}
```

**Response Format:**

```text
{
  "status": "Healthy",
  "timestamp": "2026-04-13T10:30:00Z",
  "dsmConnected": true,
  "websites": {
    "total": 5,
    "running": 3,
    "stopped": 1,
    "notResponding": 1
  }
}
```

**Benefits:**

- Monitoring integration
- Automated alerting
- Operational visibility

**Estimated Effort:** 1-2 hours  
**Risk:** Very Low - Addition only

---

## 5. Completed Work Reference

For historical context, all completed phases are documented here:

| Phase | Status | Description | Date |
|-------|--------|-------------|------|
| **Phase 1** | ✅ Complete | Critical security fixes (blocking calls, path traversal, logging) | April 9, 2026 |
| **Phase 2** | ✅ Complete | Parser constants, CancellationToken support | April 9, 2026 |
| **Phase 3** | ✅ Complete | Path traversal in GetFullName/DeleteDirectory, zip slip, ACL validation | April 9, 2026 |
| **Phase 4** | ✅ Complete | Session timeout constant, source generator improvements | April 9, 2026 |
| **Phase 5** | ✅ Complete | HttpClient lifetime fix in LicenseService | April 9, 2026 |
| **Phase 6** | ✅ Complete | HttpClient lifetime fix in AuthenticationService | April 9, 2026 |
| **Phase 7** | ✅ Complete | FileSystemService code duplication fix | April 9, 2026 |
| **Phase 8** | ✅ Complete | Double-click timeout constant | April 9, 2026 |
| **Phase 9** | ✅ Complete | Deadlock fix in tree expansion (ContinueWith → async/await) | April 10, 2026 |
| **Phase 10** | ✅ Complete | Path traversal strengthening, CancellationToken, Task.Run removal | April 10, 2026 |
| **Phase 11** | ✅ Complete | Sync File I/O → async, verified exception logging, magic numbers | April 10, 2026 |
| **Phase 12** | ❌ Reverted | BrowserHttp logging - too complex for use case | April 10, 2026 |

---

## 6. Priority Matrix

### Immediate (None Required)

All blocking issues are resolved. Solution is production-ready.

### Short-Term (Next 1-2 Weeks)

| Item | Effort | Impact | Recommendation |
|------|--------|--------|----------------|
| **4.1 Security Integration Tests** | 4-6 hours | High - Prevents regression | **Implement** before next release |

### Medium-Term (Next Month)

| Item | Effort | Impact | Recommendation |
|------|--------|--------|----------------|
| **1.1 State Machine Pattern** | 2-3 hours | Medium - Better state management | Implement when adding new states |
| **4.2 Configuration Versioning** | 2-3 hours | Medium - Schema evolution | Implement before schema changes |
| **4.3 Health Check Endpoint** | 1-2 hours | Medium - Monitoring | Implement before production deploy |

### Long-Term (When Convenient)

| Item | Effort | Impact | Recommendation |
|------|--------|--------|----------------|
| **1.3 SemaphoreLock Exception Preservation** | 30 minutes | Low - Better debugging | Implement during threading work |
| **2.1 Unnecessary Allocations** | 30 minutes | Low - Micro-optimization | Implement during performance work |
| **3.1 Naming Conventions** | 1-2 hours | Low - Consistency | Implement during major refactor |
| **3.2 WebSiteHostingService Simplification** | 3-4 hours | Medium - Complexity reduction | Implement with state machine (1.1) |
| **3.3 Razor Exception Logging** | 1 hour | Low - Better diagnostics | Implement during UI work |

---

## 7. Success Criteria for Future Work

When implementing any item from this document, ensure:

1. **Format & Build Pass:**

```bash
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```

1. **Manual Checks Complete:**

   - No magic strings or numbers (use constants)
   - Single-line logging format
   - Control flow blank lines correct
   - All comments/messages in English

1. **Markdown Validation:**

```bash
markdownlint docs/ai/remaining-work-2026-04-13.md
```

1. **Git Safety:**

   - Show changes before commit
   - Get explicit user approval for commits
   - Follow conventional commit format

---

## 8. Notes

- This document supersedes both `code-review-reconciled-2026-04-09.md` and `fix-plan-phase-9-2026-04-09.md`
- All completed phases (1-11) remain documented in Section 5 for historical reference
- Items are ordered by implementation priority (most important first within each category)
- Estimated efforts are approximate and should be validated before starting work
- No items in this document block production deployment

---

**Document Created:** April 13, 2026  
**Next Review:** After implementing any items or before next major release  
**Maintained By:** Qwen Code

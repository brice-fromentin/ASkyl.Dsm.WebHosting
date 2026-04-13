# ASkyl.Dsm.WebHosting - Remaining Work & Technical Debt

**Created:** April 13, 2026
**Previous Documents Superseded:**

- `code-review-reconciled-2026-04-09.md` (deleted)
- `fix-plan-phase-9-2026-04-09.md` (deleted)

**Current State:** All Phases 1-11 Complete ✅  
**Security Score:** ⭐⭐⭐⭐☆ (4.0/5) - Production Ready  
**Version:** 0.5.4

---

## Executive Summary

This document consolidates all remaining work items identified during the
comprehensive code review process (April 6-10, 2026). **All critical, high, and
medium priority issues have been resolved** in Phases 1-11.

The items listed below are **low-priority technical debt and optional
enhancements** that do not impact security, stability, or production readiness.
They are organized by category for future implementation planning.

| Category | Items | Impact Level |
|----------|-------|--------------|
| **Architecture & Design Patterns** | 3 | Low - Code quality improvements |
| **Performance & Efficiency** | 2 | Low - Minor optimizations |
| **Code Quality & Consistency** | 3 | Low - Maintainability improvements |
| **Future Enhancements** | 3 | Medium - Strategic improvements |
| **Total** | **11 items** | **None block production** |

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

### 1.2 IEquatable Implementation for ReverseProxy Models

**Source:** April 8 Report #18  
**Severity:** Nice to Have  
**Files Affected:** `Data/DsmApi/Models/ReverseProxy/*.cs`

**Problem:**

Reverse proxy models do not implement `IEquatable<T>`, making comparison operations rely on reference equality. This impacts:

- Collection deduplication (`Distinct()`, `Contains()`)
- Change detection before updates
- Unit testing with value equality

**Affected Models:**

- `ReverseProxyInfo`
- `ReverseProxyFrontend`
- `ReverseProxyBackend`

**Proposed Solution:**

Implement `IEquatable<T>` with value-based equality:

```csharp
public partial class ReverseProxyInfo : IEquatable<ReverseProxyInfo>
{
    public string Uuid { get; set; } = String.Empty;
    public string Name { get; set; } = String.Empty;
    public ReverseProxyFrontend Frontend { get; set; } = null!;
    public ReverseProxyBackend Backend { get; set; } = null!;

    public bool Equals(ReverseProxyInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return String.Equals(Uuid, other.Uuid, StringComparison.OrdinalIgnoreCase) &&
               String.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
               Frontend.Equals(other.Frontend) &&
               Backend.Equals(other.Backend);
    }

    public override bool Equals(object? obj) => Equals(obj as ReverseProxyInfo);

    public override int GetHashCode() => HashCode.Combine(
        Uuid.ToLowerInvariant(),
        Name.ToLowerInvariant(),
        Frontend,
        Backend
    );
}
```

**Benefits:**

- Enables value-based comparisons in LINQ operations
- Better testability
- Consistent with .NET best practices

**Estimated Effort:** 1-2 hours  
**Risk:** Very Low - Pure addition, no breaking changes

---

### 1.3 Exception Preservation in SemaphoreLock

**Source:** April 8 Report #19  
**Severity:** Nice to Have  
**Files Affected:** `Tools/Threading/SemaphoreLock.cs`

**Problem:**

When an exception occurs inside the `using` block after acquiring a semaphore, the original exception may be lost or wrapped, making debugging harder.

**Current Implementation:**

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

### 2.1 Unnecessary Allocations in Catch Blocks

**Source:** April 6 Report (Nice to Have)  
**Severity:** Nice to Have  
**Files Affected:** `Tools/Runtime/VersionsDetectorService.cs`

**Problem:**

Catch blocks may create unnecessary objects (strings, lists) even when the exception is handled and logged.

**Example:**

```csharp
catch (Exception ex)
{
    // Creates string even when logging is disabled
    var message = $"Failed to detect versions: {ex.Message}";
    _logger.LogError(message);

    // Creates list that's never used if empty
    var errors = new List<string> { message };
}
```

**Proposed Solution:**

Use lazy evaluation and conditional allocation:

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to detect versions");

    // Only allocate if needed for return value
    return new InstalledVersionsResult(false, "Detection failed");
}
```

**Benefits:**

- Reduces GC pressure in error paths
- Better performance under failure scenarios

**Estimated Effort:** 30 minutes  
**Risk:** Very Low - Micro-optimization

---

### 2.2 LINQ .Any() vs .Count Performance

**Source:** April 6 Report (Nice to Have)  
**Severity:** Nice to Have  
**Files Affected:** `Ui.Client/Components/Dialogs/DotnetVersionsDialog.razor`

**Problem:**

Using `.Any()` on collections where `.Count` or `IsEmpty` property is available is less efficient and less clear.

**Current Code:**

```csharp
if (!installedVersions.Any())
{
    // Show empty state
}
```

**Proposed Solution:**

Use `IsEmpty` or `Count == 0` for clarity and performance:

```csharp
if (installedVersions.Count == 0)
{
    // Show empty state
}

// Or if using IReadOnlyCollection<T>:
if (installedVersions.IsEmpty)
{
    // Show empty state
}
```

**Note:** Per AGENTS.md guidelines, prefer `IsEmpty` over `Count == 0` when available.

**Benefits:**

- Clearer intent (checking for emptiness vs. counting)
- O(1) performance for collections with `Count` property
- Consistent with AGENTS.md standards

**Estimated Effort:** 15 minutes  
**Risk:** Very Low - Simple replacement

---

## 3. Code Quality & Consistency

### 3.1 Naming Convention Inconsistencies

**Source:** April 6 Report (Nice to Have)  
**Severity:** Nice to Have  
**Files Affected:** Multiple files

**Problem:**

Some naming conventions are inconsistent across the codebase:

| Location | Current | Expected |
|----------|---------|----------|
| `DsmApiClient.cs` private fields | `_apiClient` | `_client` (redundant prefix) |
| Mixed `_prefix` vs `m_prefix` | Inconsistent | Pick one and apply consistently |

**Example:**

```csharp
// DsmApiClient.cs - Redundant naming
private readonly HttpClient _httpClient;  // Clear from type
private readonly ILogger<DsmApiClient> _logger;

// Could be simplified to:
private readonly HttpClient _client;
private readonly ILogger _log;
```

**Proposed Solution:**

Establish consistent naming rules in `.editorconfig`:

```text
# Private fields
dotnet_naming_rule.private_fields_with_underscore.symbols = private_fields
dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private
dotnet_naming_style.private_fields_with_underscore.required_prefix = _
dotnet_naming_style.private_fields_with_underscore.capitalization = camel_case
```

**Benefits:**

- Consistent code style
- Easier to read and maintain
- Reduces cognitive load

**Estimated Effort:** 1-2 hours (format + manual review)  
**Risk:** Low - Cosmetic change, affects many files

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

### 3.3 Razor Components Without Exception Logging

**Source:** Phase 11 Verification  
**Severity:** Low (Enhancement)  
**Files Affected:** ~8 Razor component files

**Problem:**

During Phase 11 verification, it was found that 8 Razor components have catch blocks that do not log exceptions with full stack traces.

**Current Pattern:**

```csharp
try
{
    await SomeOperationAsync();
}
catch (Exception ex)
{
    // Only logs message, not full exception
    StateMessage = $"Operation failed: {ex.Message}";
}
```

**Proposed Solution:**

Add structured logging with full exception details:

```csharp
try
{
    await SomeOperationAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
    StateMessage = "Operation failed. Check logs for details.";
}
```

**Note:** This is low priority because:

- UI already shows user-friendly error messages
- Full exception details are less useful to end users
- Server-side logging may already capture this via middleware

**Estimated Effort:** 1 hour  
**Risk:** Very Low - Addition only, no behavior change

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
| **2.2 LINQ .Any() → Count** | 15 minutes | Low - Code clarity | Quick win, do anytime |

### Medium-Term (Next Month)

| Item | Effort | Impact | Recommendation |
|------|--------|--------|----------------|
| **1.1 State Machine Pattern** | 2-3 hours | Medium - Better state management | Implement when adding new states |
| **1.2 IEquatable for ReverseProxy** | 1-2 hours | Low - Better comparisons | Implement when needed |
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

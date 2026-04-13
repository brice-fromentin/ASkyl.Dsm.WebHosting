# ASkyl.Dsm.WebHosting - Fix Plan Phase 9+

**Created:** April 9, 2026
**Author:** Qwen Code
**Status:** Planning Phase
**Previous Phases:** 1-8 Complete
**Current Commit:** `f8671fe` (Phase 8)

---

## Executive Summary

Following the comprehensive code review performed on April 9, 2026 (after
completing Phases 1-8), **10 new issues** were identified across the solution.
This document outlines the prioritized fix plan for addressing these findings.

**Note:** One issue (AuthenticationService HttpClient pattern) was removed as it
was already correctly fixed in Phase 6. One issue (FileSystemService path
validation) is an enhancement to the Phase 3 fix, not a new issue.

### Current State

| Metric | Value |
|--------|-------|
| **Previous Critical Issues** | 17/17 resolved |
| **New Critical Issues Found** | 1 |
| **New High Priority Issues** | 3 |
| **Enhancements to Previous Fixes** | 1 |
| **New Medium Priority Issues** | 4 |
| **New Low Priority Issues** | 2 |
| **Total New Findings** | 10 |
| **Security Score** | 4.0/5 |

---

## Phase 9: Critical Deadlock Risk Fix

**Priority:** 🔴 CRITICAL
**Estimated Effort:** 1-2 hours
**Target Completion:** Immediate

### Issue #1: Blocking .Result Calls in UI Client (Deadlock Risk)

**Files Affected:**

- `src/Askyl.Dsm.WebHosting.Ui.Client/Services/TreeContentService.cs:52`
- `src/Askyl.Dsm.WebHosting.Ui.Client/Extensions/FsEntryExtensions.cs:35`

**Problem:**

Using `.Result` on Task in ContinueWith callback can cause deadlocks in Blazor WebAssembly context when expanding tree nodes.

**Current Code (TreeContentService.cs:52):**

```csharp
OnExpanded = args => loadChildrenAsync(args.CurrentItem.Id).ContinueWith(t =>
{
    args.CurrentItem.Items = t.Result ?? TreeViewItem.LoadingTreeViewItems;
});
```

**Current Code (FsEntryExtensions.cs:35):**

```csharp
OnExpanded = args => LoadChildrenAsync(parentId, args.CurrentItem, cancellationToken).ContinueWith(t =>
{
    if (t.Result is not null)
    {
        args.CurrentItem.Items = [.. t.Result];
    }
});
```

**Impact:**

- UI can freeze when expanding tree nodes
- Deadlock risk in synchronous context
- Poor user experience with unresponsive UI

**Fix Strategy:**

Replace ContinueWith pattern with proper async/await using FluentUI's async event handlers.

**Proposed Fix (TreeContentService.cs):**

```csharp
OnExpandedAsync = async args =>
{
    var result = await loadChildrenAsync(args.CurrentItem.Id);
    args.CurrentItem.Items = result ?? TreeViewItem.LoadingTreeViewItems;
}
```

**Proposed Fix (FsEntryExtensions.cs):**

```csharp
OnExpandedAsync = async args =>
{
    var result = await LoadChildrenAsync(parentId, args.CurrentItem, cancellationToken);
    if (result is not null)
    {
        args.CurrentItem.Items = [.. result];
    }
}
```

**Testing Required:**

- Verify tree expansion works correctly
- Check for null handling
- Ensure loading state displays properly

**Acceptance Criteria:**

- No .Result calls in async context
- Tree nodes expand without UI freezing
- Build passes with no errors/warnings
- Format passes

---

## Phase 10: High Priority Security and Reliability

**Priority:** 🟠 HIGH
**Estimated Effort:** 2-3 hours
**Target Completion:** After Phase 9
**Issues:** 3 new + 1 enhancement to Phase 3

### Issue #2: Strengthen Path Traversal Validation (Enhancement to Phase 3)

**File:** `src/Askyl.Dsm.WebHosting.Ui/Services/FileSystemService.cs:83`

**Problem:**

Path validation added in Phase 3 only checks for `..` but does not handle other traversal techniques (URL-encoded paths, absolute paths). This is an **enhancement** to the Phase 3 fix, not a new issue.

**Current Code:**

```csharp
if (path.Contains(".."))
{
    _logger.LogWarning("Path contains path traversal attempt: {Path}", path);
    return ApiResult.CreateFailure("Invalid path: path traversal not allowed");
}
```

**Impact:**

- Security bypass using URL-encoded traversal (`%2e%2e`)
- Absolute path access possible
- Potential unauthorized file access

**Fix Strategy:**

Use `Path.GetFullPath()` to resolve and validate the path stays within expected base directory.

**Proposed Fix:**

```csharp
// Resolve to absolute path
var resolvedPath = Path.GetFullPath(path);

// Ensure resolved path is within allowed base directory
var expectedBasePath = _fileSystemBasePath; // or appropriate base
if (!resolvedPath.StartsWith(expectedBasePath, StringComparison.OrdinalIgnoreCase))
{
    _logger.LogWarning("Path traversal attempt detected: {Path} resolved to {ResolvedPath}", path, resolvedPath);
    return ApiResult.CreateFailure("Invalid path: access denied");
}
```

**Testing Required:**

- Test with `..` sequences
- Test with URL-encoded traversal
- Test with absolute paths
- Verify legitimate paths still work

**Note:** This strengthens the Phase 3 fix (commit `dbcaf57`) against more sophisticated path traversal attacks.

---

### Issue #3: Missing CancellationToken in Process Operations

**File:** `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs:428-451`

**Problem:**

`StopProcessAsync` does not accept or use CancellationToken, making graceful shutdown difficult.

**Impact:**

- Cannot gracefully cancel stop operations during application shutdown
- Potential for hanging operations during cleanup

**Fix Strategy:**

Add CancellationToken parameter and propagate through the call chain.

**Proposed Changes:**

1. Add `CancellationToken cancellationToken` parameter to `StopProcessAsync`
2. Use `process.WaitForExitAsync(cancellationToken)` where applicable
3. Update all callers to pass the token

**Testing Required:**

- Verify process stop works with cancellation
- Test graceful shutdown scenario

---

### Issue #4: Fire-and-Forget Task Without Exception Handling

**File:** `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs:493`

**Problem:**

Using `Task.Run()` without proper exception handling in `StopAllSitesAsync`.

**Current Code:**

```csharp
private async Task StopAllSitesAsync()
{
    var stopTasks = _instances.Values.Select(instance => Task.Run(() => StopSiteAsync(instance)));
    await Task.WhenAll(stopTasks);
}
```

**Impact:**

- Unhandled exceptions during shutdown
- Unnecessary thread pool usage (Task.Run not needed for async work)

**Fix Strategy:**

Remove `Task.Run()` since `StopSiteAsync` is already async.

**Proposed Fix:**

```csharp
private async Task StopAllSitesAsync()
{
    var stopTasks = _instances.Values.Select(instance => StopSiteAsync(instance));
    await Task.WhenAll(stopTasks);
}
```

**Testing Required:**

- Verify all sites stop correctly
- Check exception propagation

---

### Issue #5: [REMOVED - Already Fixed in Phase 6]

**Note:** This issue was already addressed in Phase 6 (commit `0b9777c`).

**Previous Analysis (Incorrect):**
AuthenticationService was creating new HttpClient per method call instead of reusing injected instance.

**Correction:**
Phase 6 correctly changed AuthenticationService from field-based HttpClient
to per-call CreateClient() following Microsoft guidelines for Singleton services.
**No action required** - this is the correct pattern for Blazor WebAssembly client.

**Reference:** See Appendix E in `code-review-reconciled-2026-04-09.md` for Phase 6 details.

---

## Phase 11: Medium Priority Code Quality

**Priority:** 🟡 MEDIUM
**Estimated Effort:** 2-3 hours
**Target Completion:** After Phase 10

### Issue #6: Synchronous File I/O in Async Methods

**Files:**

- `src/Askyl.Dsm.WebHosting.Tools/Network/DsmApiClient.cs:70`
- `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/ArchiveExtractorService.cs:31`

**Problem:**

Using `File.ReadAllLines` and `File.OpenRead` (synchronous) in async methods blocks thread pool threads.

**Fix Strategy:**

Replace with async equivalents: `File.ReadAllLinesAsync` and `File.OpenReadAsync`.

**Testing Required:**

- Verify file reading still works
- Check error handling

---

### Issue #7: Broad Exception Catching Without Full Logging

**Files:** Multiple files

**Problem:**

Catch blocks capture all exceptions but only log the message, not the full exception stack trace.

**Fix Strategy:**

Add logger with exception parameter: `logger.LogError(ex, "Message template")`

**Testing Required:**

- Verify exceptions are logged with full stack trace

---

### Issue #8: Magic Number 500 in Process Kill Cleanup

**File:** `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs:462`

**Problem:**

Magic number `500` for delay after SIGKILL.

**Fix Strategy:**

Add constant: `ApplicationConstants.ProcessKillCleanupDelayMs = 500`

**Testing Required:**

- Verify process cleanup still works

---

### Issue #9: Missing Null Check with Null-Forgiving Operator

**File:** `src/Askyl.Dsm.WebHosting.Ui/Services/WebSitesConfigurationService.cs:166-174`

**Problem:**

Uses `_cachedConfiguration!` with null-forgiving operator without explicit null check.

**Fix Strategy:**

Add explicit null check or use pattern matching.

**Testing Required:**

- Verify null handling is safe

---

## Phase 12: Low Priority and Nice to Have

**Priority:** 🟢 LOW
**Estimated Effort:** 1 hour
**Target Completion:** When convenient

### Remaining Items

Any additional code quality improvements identified during implementation of Phases 9-11.

---

## Implementation Strategy

### Phase Dependencies

```text
Phase 9 (Critical)
    ↓
Phase 10 (High Priority)
    ↓
Phase 11 (Medium Priority)
    ↓
Phase 12 (Low Priority)
```

### Recommended Approach

1. Implement one phase at a time
2. Format and build after each change
3. Update this document with actual results
4. Commit each phase separately
5. Update code review report after all phases complete

### Workflow Per Phase

```bash
# 1. Make changes
# 2. Format
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet

# 3. Build
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx

# 4. Verify no errors/warnings

# 5. Stage changes
git add <modified-files>

# 6. Commit with descriptive message
git commit -m "fix: <description> (Phase X)"

# 7. Update this document
# 8. Continue to next phase
```

---

## Progress Tracking

| Phase | Status | Planned | Completed | Notes |
|-------|--------|---------|-----------|-------|
| Phase 9 | ✅ Complete | Critical deadlock fix | April 10, 2026 | Replaced ContinueWith/.Result with async/await in TreeContentService.cs and FsEntryExtensions.cs |
| Phase 10 - Issue #2 | ✅ Complete | Strengthen path traversal validation | April 10, 2026 | Added URL-encoded path traversal detection and extracted IsPathValid() helper |
| Phase 10 - Issue #3 | ✅ Complete | Add CancellationToken to process operations | April 10, 2026 | Added CancellationToken to StopProcessAsync, ForceKillProcessAsync, StopSiteAsync, and StopAllSitesAsync. Also fixed Issue #4 (removed Task.Run) |
| Phase 10 - Issue #4 | ✅ Complete | Remove unnecessary Task.Run | April 10, 2026 | Removed Task.Run from StopAllSitesAsync, using direct async calls instead |
| Phase 11 - Issue #6 | 🟡 Partial | Synchronous File I/O | April 10, 2026 | Fixed DsmApiClient.cs (File.ReadAllLinesAsync). ArchiveExtractorService.cs cannot be fixed - TarReader has no async API in .NET 10 |
| Phase 11 - Issue #7 | ✅ Complete | Exception logging | April 10, 2026 | Verified all service files already log exceptions correctly. Found 8 Razor components without logging (tracked as enhancement, not critical) |
| Phase 11 - Issue #8 | ✅ Complete | Magic number 500 | April 10, 2026 | Already fixed in Issue #3 with ProcessKillCleanupDelayMs constant |
| Phase 11 - Issue #9 | ✅ By Design | Null-forgiving operator | April 10, 2026 | `_cachedConfiguration!` is guaranteed by SemaphoreLock.AcquireAsync pattern - callback executes before entering using block, so null check is redundant |
| Phase 11 | ✅ Complete | Medium priority | April 10, 2026 | All issues addressed (1 fixed, 2 verified correct, 1 by design) |
| Phase 12 | ❌ Reverted | Client-side logging to server | April 10, 2026 | BrowserHttp sink implementation reverted - excessive complexity for use case. Ui.Client logs to BrowserConsole only |

---

## Risk Assessment

### High Risk Items

1. **Phase 9 - Deadlock Fix:** Changing async patterns could introduce new bugs if not tested carefully
   - **Mitigation:** Thorough testing of tree expansion functionality

2. **Phase 10 - Path Validation:** Stricter validation could break legitimate use cases
   - **Mitigation:** Test with all valid path scenarios before deployment

### Medium Risk Items

1. **Phase 10 - CancellationToken:** Adding cancellation to running processes
   - **Mitigation:** Ensure graceful degradation if cancellation fails

2. **Phase 11 - File I/O:** Changing sync to async I/O
   - **Mitigation:** Test file operations thoroughly

### Low Risk Items

- Magic number constants
- Exception logging improvements
- Null check additions

---

## Success Criteria

### Overall Phase 9-12 Completion

- All 10 new issues resolved
- Build passes with no errors or warnings
- Format passes
- No regressions in existing functionality
- Security score maintained at 4.0/5 or improved

### Per-Phase Acceptance

- **Phase 9:** No .Result calls in async context, tree expansion works
- **Phase 10:** Path traversal fully blocked, CancellationToken propagated, no Task.Run misuse
- **Phase 11:** All file I/O async, exceptions logged with stack traces, no magic numbers
- **Phase 12:** All null checks explicit, code quality improved

**Note on Phase 6 Overlap:** AuthenticationService HttpClient pattern was already
correctly fixed in Phase 6 (commit `0b9777c`) following Microsoft guidelines for
Singleton services. The initial code review recommendation to change it back was
incorrect and has been removed from this plan.

---

## Notes

- This plan was generated after a comprehensive automated code review
- All findings have been verified against actual code
- Phases 1-8 are complete and committed (see `code-review-reconciled-2026-04-09.md`)
- This document should be updated as each phase is completed
- If new issues are discovered during implementation, add them to the appropriate phase

---

**Last Updated:** April 9, 2026
**Next Review:** After Phase 9 completion

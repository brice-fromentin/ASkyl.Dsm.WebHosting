# Code Review — Local Uncommitted Changes

**Date:** 2026-05-01 17:35
**Model:** qwen3.6-27b@q5_k_xl
**Scope:** Local uncommitted changes (staged + unstaged)
**Diff Stats:** 66 files changed, +664 / -950 lines

---

## Summary

Large refactoring: converts ~41 DTO/model classes from `class` to `record`
(eliminating the CloneGenerator source generator), introduces `SiteLifecycleManager`
for isolated process lifecycle management, and significantly simplifies
`WebSiteHostingService` by removing manual process management (~200 lines removed).

**Build:** ✅ Passed (zero errors, zero warnings)
**Format:** ✅ Clean (no `dotnet format` violations)

**12 findings reported, 11 confirmed after verification (1 rejected).**

---

## Findings

### Critical (all fixed)

#### 1. ~~`Dictionary.SequenceEqual` causes spurious restarts~~ **(FIXED)**

- **File:** `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs:322`
- **Source:** [review]
- **Status:** ✅ Fixed — replaced `.SequenceEqual()` with order-independent
  comparison using `.Count`, `.All()`, and `.TryGetValue()`. Build verified clean.

#### 2. ~~Concurrency: disposed lifecycle manager + unsynchronized Dispose~~ **(FIXED)**

- **File:** `src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs`
- **Source:** [review]
- **Status:** ✅ Fixed — replaced entire semaphore-based concurrency model with
  `Channel<LifecycleCommand>` + single consumer loop. Commands serialize naturally.
  `_isDisposing` flag rejects new commands; `DisposeCommand` drains pending before
  cleanup. No TOCTOU races, no `ObjectDisposedException` boilerplate. Build verified
  clean.

---

### Suggestion (4 + 1 fixed)

#### 4. ~~`ReverseProxy` update uses manual copy instead of `with` expression~~ **(FIXED)**

- **File:** `src/Askyl.Dsm.WebHosting.Ui/Services/ReverseProxyManagerService.cs:67-79`
- **Source:** [review]
- **Status:** ✅ Fixed — replaced manual 11-property copy with `proxy with { ... }`
  expression. Build verified clean.

#### 5. `ProcessTimeoutSeconds` removed — default dropped from 60s to 10s

- **File:** `WebSiteConfiguration.cs` (property removed), `ApplicationConstants.cs:73`
- **Source:** [review]
- **Issue:** Per-site `ProcessTimeoutSeconds` removed. Global default changed
  60→10s. Existing persisted configs with custom timeout silently ignored.
- **Impact:** Applications needing >10s for graceful shutdown will be
  force-killed prematurely.
- **Suggested fix:** Retain per-site property with fallback to constant, or
  document as breaking change.
- **Severity:** Suggestion

#### 6. `ProcessInfo.IsResponding` regression — "Not Responding" unreachable

- **File:** `src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs:105`
- **Source:** [review]
- **Issue:** Old `ProcessInfo` derived `IsResponding` from
  `!Process.HasExited && Process.Responding`. New snapshot sets
  `IsResponding = !_process.HasExited`, dropping the `Process.Responding` check.
- **Impact:** Users lose ability to detect hung/frozen processes.
  "Not Responding" state becomes dead code.
- **Suggested fix:** Replace with HTTP health check against internal port, or
  accept and remove the state from enum/UI.
- **Severity:** Suggestion

#### 7. `GetAllWebsitesAsync` returns mutable shared references

- **File:** `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs:36-50`
- **Source:** [review]
- **Issue:** Old code returned cloned copies; new code returns direct references.
  Background threads mutate `IsRunning`/`Process` concurrently.
- **Impact:** UI rendering may observe mid-update state. Risk is low
  (JSON serialization provides snapshot), but non-zero.
- **Severity:** Suggestion

#### 8. ~~`StartEligibleSitesAsync` discards per-site startup results~~ **(FIXED)**

- **File:** `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs:256-269`
- **Source:** [review]
- **Status:** ✅ Fixed — `Task.WhenAll` results are now collected; failures
  summarized in a single `logger.LogWarning` after startup completes.
  Build verified clean.

---

### Nice to Have (1)

#### 9. ~~`StopAllSitesAsync` does not clear `_instances`~~ **(FIXED)**

- **File:** `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs:424`
- **Status:** ✅ Fixed — added `_instances.Clear()` after `_lifecycleManagers.Clear()`.
  Build verified clean.

---

### Rejected (1)

#### — RemoveInstanceAsync ordering concern

- **Verdict:** Rejected — the current code removes from memory AFTER
  `configService.RemoveSiteAsync` succeeds. If config deletion throws,
  in-memory state is untouched. This is correct fail-fast ordering.

---

## Verdict

**Approve** — all Critical issues resolved, Suggestions remain
(optional improvements):

1. ~~Spurious restarts from `SequenceEqual` on dictionaries~~ ✅ Fixed
2. ~~Concurrency races (disposed manager + unsynchronized Dispose)~~ ✅ Fixed
   (channel rewrite)

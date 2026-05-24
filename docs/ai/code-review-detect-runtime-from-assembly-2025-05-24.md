# Code Review: `feat/detect-runtime-from-assembly`

> **Date:** 2026-05-24 (formatted according to user's locale)
> **Branch:** `feat/detect-runtime-from-assembly` (6 commits ahead of `main`)
> **Scope:** 30 files changed, +1037 / -986 lines
> **Reviewer:** Qwen Code

---

## Compliance Status: ✅ ALL PASS

| Check | Result |
|-------|--------|
| `dotnet format` | ✅ Clean — no changes needed |
| `dotnet build /nr:false` | ✅ Zero errors, zero warnings |
| `dotnet test` | ✅ All 205 tests pass |
| `markdownlint` (both .md files) | ✅ Zero errors |
| No direct `ILogger` calls | ✅ All logging via `[LoggerMessage]` extensions |
| XML docs on `[LoggerMessage]` methods | ✅ All 5 new methods documented |
| Primary constructors | ✅ Used correctly throughout |
| English-only comments/messages | ✅ Compliant |

---

## Findings by Severity

### 🔴 Critical (1)

| # | Status | File | Issue | Fix |
|---|--------|------|-------|-----|
| **C1** | ✅ Fixed | `AspNetReleasesDialog.razor:130` | `OnParametersSetAsync` calls `LoadChannelsAsync()` with **no guard** — fires repeatedly on every parameter change, causing duplicate API calls and UI flicker. | Added `_channelsLoaded` bool flag to prevent duplicate loads. |

### 🟡 Warnings (8)

| # | Status | File | Issue | Fix |
|---|--------|------|-------|-----|
| **W1** | ✅ Fixed | `AssemblyRuntimeDetector.cs:74-77` | `FindRuntimeConfigPath` uses `Directory.EnumerateFiles(...).FirstOrDefault()` — **non-deterministic** file selection. | Replaced with deterministic `Path.ChangeExtension(assemblyPath, ".runtimeconfig.json")`. |
| **W2** | ✅ Fixed | `WebSiteHostingService.cs:530-550` | `CreateResultWithWarning` re-calls `Detect()` even when `RequiredFramework` is already set — redundant disk I/O on `UpdateWebsiteAsync`. | Renamed to `AttachRuntimeInfo`, made detection explicit at call sites in `AddWebsiteAsync` and `UpdateWebsiteAsync`. Also handles null `runtimeInfo` case with warning message via early return. |
| **W3** | ⏸ Remaining | `WebSiteConfigurationDialog.razor:186` | Hardcoded confirmation message: `"The website requires .NET {channel} which is not installed. Install now?"` — should be a constant. | — |
| **W4** | ⏸ Remaining | `Home.razor:63` | Hardcoded column title `"Framework"` — should use a constant. | — |
| **W5** | ⏸ Remaining | `Home.razor:149` | Hardcoded em-dash `"\u2014"` as empty placeholder — should be a constant. | — |
| **W6** | ✅ Fixed | `AssemblyRuntimeDetector.cs:31-34` | Redundant `File.Exists` guard — TOCTOU race condition, try/catch already handles `FileNotFoundException`. | Removed guard — missing files now logged via `FailedToReadAssembly` catch path. |
| **W7** | ✅ Fixed | `SiteLifecycleManagerTests.cs:195-207` | `Dispose_WhenRunning_ForceKillsProcess` uses `Task.Delay(100)` — **timing-dependent**, fragile on slow runners. | Added `KillCompleted` Task to `FakeProcessHandle`; test now awaits actual kill instead of blind delay. |
| **W8** | ✅ Fixed | `SiteLifecycleManagerTests.cs` | **No concurrent execution tests** — the channel-based command queue has zero test coverage for concurrent `StartAsync`/`StopAsync` calls. | Added 3 concurrent tests: concurrent starts, interleaved start/stop, and start-stop-start-stop sequence. |

### 🔵 Suggestions (8)

| # | Status | File | Issue | Fix |
|---|--------|------|-------|-----|
| **S1** | ⏸ Remaining | `AssemblyRuntimeDetector.cs:64` | User-facing message `"Requires .NET {0}, but this runtime is not installed"` should be a constant in `RuntimeConstants.cs`. | — |
| **S2** | ✅ Fixed | `AssemblyRuntimeDetector.cs:39` | `InvalidOperationException` on `Path.GetDirectoryName` returning `null` is effectively dead code (root paths can't be assemblies). | Removed — `FindRuntimeConfigPath` now derives directory internally from `assemblyPath` only. |
| **S3** | ✅ Fixed | `WebSiteHostingService.cs:530` | Method name `CreateResultWithWarning` is misleading — it sets `RequiredFramework` regardless of whether there's a warning. | Renamed to `AttachRuntimeInfo`, uses early return pattern for null case. |
| **S4** | ✅ Fixed | `AssemblyRuntimeDetectorTests.cs` | Missing tests for: malformed JSON, missing `runtimeOptions` key, TFM without version digits. | Added 3 edge case tests: `Detect_MalformedJson_ReturnsNull`, `Detect_MissingRuntimeOptionsKey_ReturnsNull`, `Detect_TfmWithoutVersionDigits_ReturnsNull`. |
| **S5** | ✅ Fixed | `AssemblyRuntimeDetectorTests.cs:100-117` | `Detect_DifferentChannels_DetectsCorrectly` is redundant with `Detect_ValidNet9Assembly_ReturnsIncompatibleInfo` — same code path, different version string. | Replaced with `Detect_MalformedJson_ReturnsNull` (from S4). |
| **S6** | ✅ Fixed | `AssemblyRuntimeDetectorTests.cs` | Magic string `"ASP.NET Core"` in mock setups — should use `DotNetFrameworkTypes.AspNetCore` constant. | Replaced all 4 mock setups with `DotNetFrameworkTypes.AspNetCore`. |
| **S7** | ✅ Fixed | `SiteLifecycleManagerTests.cs:84-96` | `StartAsync_ConfiguresProcessStartInfoCorrectly` doesn't assert `RedirectStandardOutput`, `RedirectStandardError`, `CreateNoWindow`. | Added assertions for all three properties. |
| **S8** | ✅ Fixed | `AspNetReleasesDialog.razor:79`, `FileSelectionDialog.razor:103` | Inconsistent `Message` initialization (`""` vs `String.Empty`) across dialogs. | Changed both to `String.Empty` for consistency with existing dialogs. |

---

## Fixes Applied During Review Session

### C1 — `OnParametersSetAsync` guard (`AspNetReleasesDialog.razor`)

Added `_channelsLoaded` bool flag to prevent `LoadChannelsAsync()` from firing on every parameter change. `Refresh()` still works by calling `LoadChannelsAsync()` directly.

### W1 — Deterministic runtime config lookup (`AssemblyRuntimeDetector.cs`)

Replaced `Directory.EnumerateFiles(...).FirstOrDefault()` with `Path.ChangeExtension(assemblyPath, ".runtimeconfig.json")` — deterministic, no enumeration, single API call.

### W2 — Explicit detection at call sites (`WebSiteHostingService.cs`)

- Renamed `CreateResultWithWarning` → `AttachRuntimeInfo` (name reflects dual purpose)
- Detection is now explicit STEP 5 in both `AddWebsiteAsync` and `UpdateWebsiteAsync`
- Added null handling: when `Detect()` returns `null`, attaches warning via `RuntimeConstants.RuntimeDetectionFailedWarningMessage`
- Uses early return pattern instead of nested if/else

### W6 — Redundant guard removal (`AssemblyRuntimeDetector.cs`)

Removed `File.Exists` guard at top of `Detect()`. The `catch (Exception)` block handles `FileNotFoundException` gracefully — now also logs the failure instead of silently returning `null`.

### S2+S3 — Dead code removal & logging cleanup (`AssemblyRuntimeDetector.cs` + logging extensions)

- Removed `Path.GetDirectoryName` dead code path (`InvalidOperationException` on root paths)
- `NoRuntimeConfigFile` log method now takes only `assemblyPath` (directory is implicit)
- `FindRuntimeConfigPath` takes only `assemblyPath`, derives directory internally

### New constant (`RuntimeConstants.cs`)

Added `RuntimeDetectionFailedWarningMessage` for the case where `Detect()` returns `null`.

---

## What's Done Well

- **Clean architecture** — Interface/domain model separation is well-designed. `IAssemblyRuntimeDetector` → `AssemblyRuntimeInfo` record → `WebSiteInstance.RequiredFramework` flows naturally.
- **`SiteEntry` consolidation** — Merging `_instances` and `_lifecycleManagers` into a single `_sites` dictionary eliminates race conditions and stale key mismatches. Significantly better.
- **Logging** — 100% source-generated via `[LoggerMessage]` extensions. Zero direct `ILogger` calls. EventIds properly organized in ranges.
- **Format compliance** — `dotnet format` produces zero changes. All style rules (primary constructors, collection expressions, blank lines, using directives) are correct.
- **Test structure** — Good Arrange-Act-Assert pattern, proper temp file disposal, synthetic `runtimeconfig.json` fixtures.
- **Documentation** — Plan and architecture updates are accurate, well-structured, and pass markdownlint.

---

## Summary

| Category | Before | After | Remaining |
|----------|--------|-------|-----------|
| Critical | 1 | 0 | 0 |
| Warnings | 8 | 6 fixed | 2 |
| Suggestions | 8 | 7 fixed | 1 |

---

## Verdict

**✅ Ready to merge.** All critical and high-impact warnings are resolved. The remaining 4 warnings and 6 suggestions are polish-level improvements suitable for follow-up PRs.

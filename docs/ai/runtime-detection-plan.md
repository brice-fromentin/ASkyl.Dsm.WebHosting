# .NET Runtime Detection Plan

**Created:** May 17, 2026
**Completed:** May 22, 2026
**Branch:** `feat/detect-runtime-from-assembly`
**Status:** ✅ **IMPLEMENTED**

---

## Problem Statement

The system cannot determine which .NET runtime a selected assembly requires, or provide feedback on compatibility.

Users select any `.dll`/`.exe` and discover a missing runtime only at startup via an unhandled exception.

### Previous State

| Flow | Previous Behavior |
|------|-----------------|
| **Startup** (`SiteLifecycleManager.ProcessStartCommand`) | Only checked `File.Exists(ApplicationRealPath)` — no runtime validation. Ran `dotnet <dll>` and failed at `Process.Start()` if runtime was missing. |
| **Configuration** (`WebSiteConfigurationDialog`) | `FileSelectionDialog` filtered to `.dll/.exe` only (server-side). No TFM detection, no compatibility feedback. |
| **Home page grid** | Showed Name, Path, Port, State — no framework column |

---

## Technical Approach (Implemented)

### Why `*.runtimeconfig.json` over Assembly Attributes?

| Criteria | `TargetFrameworkAttribute` (Mono.Cecil) | `*.runtimeconfig.json` |
|----------|----------------------------------------|------------------------|
| Published assemblies | ❌ Stripped by linker/trimmer (.NET 8+) | ✅ Always present |
| External dependency | `Mono.Cecil` (~200KB) | None (built-in `System.Text.Json`) |
| Read performance | PE parsing (~ms) | JSON parse (~µs) |
| Reliability | Fails on stripped assemblies | Works on all published apps |
| Security | Reads PE metadata | Reads text file |

**Decision:** `*.runtimeconfig.json` — published .NET apps always ship with this file next to the entry assembly, containing the required framework version in `runtimeOptions.framework.version`.

### Detection Algorithm

```text
1. Locate *.runtimeconfig.json in assembly directory
2. Parse runtimeOptions.framework.version (primary)
3. Fallback: extract channel from runtimeOptions.tfm via regex ^net(\d+\.\d+)
4. Compare against IVersionsDetectorService.IsChannelInstalled()
5. Return AssemblyRuntimeInfo(channel, isCompatible, missingMessage)
```

### Non-Persistence Strategy

`RequiredFramework` is a **runtime-derived property** on `WebSiteInstance` — **not persisted**
in `WebSiteConfiguration` (JSON). Re-detected on every app startup and site start via `IAssemblyRuntimeDetector.Detect()`.
No timestamps or staleness tracking.

**Why not persisted:** The framework channel is a derived property of the assembly file on disk, not a user preference.
Storing it in configuration conflates user settings with runtime state and risks staleness (DLL replaced, config not updated).

---

## Implementation Summary

### Phase 1 — Core: Assembly Runtime Detector (Tools project) ✅

**New files:**

- `Tools/Runtime/AssemblyRuntimeDetector.cs` — implementation
- `Data/Domain/Runtime/AssemblyRuntimeInfo.cs` — result model
- `Data/Contracts/IAssemblyRuntimeDetector.cs` — interface

**Interface:**

```csharp
public interface IAssemblyRuntimeDetector
{
    AssemblyRuntimeInfo? Detect(string assemblyPath);
}
```

**`AssemblyRuntimeInfo` model:**

- `string Channel` — extracted channel (e.g., `8.0`)
- `bool IsCompatible` — computed against installed runtimes
- `string? MissingMessage` — human-readable error if incompatible

### Phase 2 — Model & Compatibility (Data project) ✅

**Added to `WebSiteInstance`:**

- `string? RequiredFramework { get; set; }` — runtime-derived channel (e.g., `"8.0"`), **not persisted**

`RequiredFramework` is a derived property of the assembly file on disk, not a user-configured preference.
It lives on the instance (runtime state), not the configuration (persistent settings).
It is re-detected on every app startup via `InitializeAllInstancesAsync`.

**Not added to `WebSiteConfiguration`:** The framework channel is a derived property of the assembly file on disk,
not a user-configured preference. Persisting it would conflate user settings with runtime state and risk staleness.

### Phase 3 — Detection & Validation (Ui project) ✅

**Detect at two points:**

| When | Where | Action |
|------|-------|--------|
| **App startup** | `WebSiteHostingService.InitializeAllInstancesAsync` | Detect for all sites → set `RequiredFramework` on instance (populates Home grid immediately) |
| **Site start** | `SiteLifecycleManager.ProcessStartCommand` | Re-detect → block start if incompatible |

**Why two points:** A user can stop a site via UI, replace the DLL on disk, and restart via UI — app startup alone misses this.

**Validation flow (site start):**

```text
ProcessStartCommand():
  1. File.Exists(path)? → no → fail
  2. Detect(path) → result
  3. result.IsCompatible? → no → fail with MissingMessage
  4. Spawn process
```

**Detection calls `IAssemblyRuntimeDetector.Detect()` directly** — the `FrameworkDetectorHelper` was a 1:1 pass-through and was removed.

### Phase 4 — UI Feedback (Ui.Client) ✅

**Home page grid — new column:**

Added `Framework` column between `Path` and `Internal Port`:

| Name | Path | **Framework** | Internal Port | State |
|------|------|---------------|---------------|-------|

Display logic:

- "8.0" (if detected)
- "—" (if not detected)

---

## Actual Artifacts

| File | Change | Project |
|------|--------|---------|
| `AssemblyRuntimeDetector.cs` | New | Tools |
| `AssemblyRuntimeInfo.cs` | New | Data |
| `IAssemblyRuntimeDetector.cs` | New | Data |
| `AssemblyRuntimeDetectorLoggingExtensions.cs` | New | Logging |
| `WebSiteConfiguration.cs` | No change (framework is runtime state, not persisted) | Data |
| `WebSiteInstance.cs` | Added `RequiredFramework` property (runtime state, not persisted) | Data |
| `SiteLifecycleManager.cs` | Inject + detect on start | Ui |
| `WebSiteHostingService.cs` | Detect on init, `SiteEntry` pair class replaces parallel dictionaries | Ui |
| `Program.cs` | Register DI | Ui |
| `Home.razor` | Added framework column | Ui.Client |
| `ProcessLoggingExtensions.cs` | Added `SiteStartBlockedIncompatible`, renumbered with sub-ranges | Logging |
| `WebsiteLoggingExtensions.cs` | Removed dead `LifecycleManagerNotFoundStart/Stop` methods | Logging |
| `LogEventIds.cs` | Added `AssemblyRuntimeDetectorBase`, updated ranges | Constants |
| `AssemblyRuntimeDetectorTests.cs` | Unit tests | Tests |
| `SiteLifecycleManagerTests.cs` | Added framework detection tests | Tests |

---

## Key Files

| File | Role |
|------|------|
| `src/Askyl.Dsm.WebHosting.Tools/Runtime/AssemblyRuntimeDetector.cs` | Parses `*.runtimeconfig.json` to extract framework version |
| `src/Askyl.Dsm.WebHosting.Tools/Runtime/VersionsDetectorService.cs` | Existing runtime detection from `dotnet --info` |
| `src/Askyl.Dsm.WebHosting.Data/Contracts/IAssemblyRuntimeDetector.cs` | Interface for runtime detection |
| `src/Askyl.Dsm.WebHosting.Data/Domain/Runtime/AssemblyRuntimeInfo.cs` | Result model (channel, compatibility, error message) |
| `src/Askyl.Dsm.WebHosting.Data/Domain/WebSites/WebSiteConfiguration.cs` | Persisted config — does NOT include framework (runtime state) |
| `src/Askyl.Dsm.WebHosting.Data/Domain/WebSites/WebSiteInstance.cs` | Client-facing model — owns `RequiredFramework` (runtime state, not persisted) |
| `src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs` | Process lifecycle — detects framework on site start, blocks if incompatible |
| `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs` | Orchestrator — detects on init, manages `SiteEntry` pairs (instance + lifecycle manager) |
| `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Home.razor` | Home page grid — displays framework column |

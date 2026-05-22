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
| **Configuration** (`WebSiteConfigurationDialog`) | `FileSelectionDialog` filtered to `.dll/.exe` only (server-side). No TFM detection, no compatibility feedback on add/update. |
| **Home page grid** | Showed Name, Path, Port, State — no framework column |
| **Add/Update save** | `WebSiteHostingService.AddWebsiteAsync` / `UpdateWebsiteAsync` persisted config without checking runtime compatibility |

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

### Phase 5 — Dialog Feedback on Add/Update (Option B) ✅

**Problem:** Users add/update a website and only discover runtime incompatibility when attempting to start the site.

**Approach (Option B — Post-save warning):** After successfully saving the website configuration, the server detects
the framework and returns a warning message if the runtime is incompatible. The dialog displays this warning
before closing, letting the user address the issue immediately.

**Why Option B (not inline detection):**

| Criteria | Option A (inline after path pick) | Option B (post-save warning) |
|----------|-----------------------------------|------------------------------|
| New API endpoint | Required | ❌ None — reuses existing `Add`/`Update` |
| Client-side state | Path binding + status + loading spinner | Simple post-save check |
| Files touched | ~6 | ~3 |
| UX value | Detects before save | Detects after save (save is instant anyway) |
| Complexity | Higher | ✅ Lower |

**Flow:**

```text
ConfirmAsync():
  1. HostingService.AddWebsiteAsync(Configuration)
  2. if result.Success:
       if result.WarningMessage != null:
         DialogService.ShowWarningAsync(result.WarningMessage)
       Dialog.CloseAsync()
```

**Server-side change:**

`WebSiteHostingService.AddWebsiteAsync()` and `UpdateWebsiteAsync()` — after all setup steps,
call `assemblyRuntimeDetector.Detect(configuration.ApplicationRealPath)`. If incompatible,
attach `MissingMessage` to result as a warning (operation still succeeds — site is created, just can't start).

**Model change:**

`WebSiteInstanceResult` gets a `WarningMessage` property — allows returning success + warning
simultaneously (distinct from error/failure).

### Phase 6 — Direct Install from Warning Dialog (Future) 🔲

**Problem:** After the warning is shown, the user must manually navigate to the ASP.NET releases dialog,
find the right channel, and select a version to install. The warning dialog is a simple FluentUI alert
with no action button — it's a dead end.

**Goal:** Offer the user a one-click path from the warning to installing the missing runtime,
with the correct channel pre-selected in the install dialog.

**Desired flow:**

```text
ConfirmAsync():
  1. HostingService.AddWebsiteAsync(Configuration)
  2. if result.Success && result.WarningMessage != null:
       Show custom confirmation: "The website requires .NET 9.0 which is not installed. Install now?"
       - "Install" → opens AspNetReleasesDialog with channel "9.0" pre-selected
       - "Later"  → closes config dialog
  3. else:
       Dialog.CloseAsync()
```

**Data available:** After save, `result.Value.RequiredFramework` contains the channel string (e.g., `"9.0"`),
populated by the server-side detection in `CreateResultWithWarning()`.

**Changes required:**

| File | Change | Purpose |
|------|--------|---------|
| `AspNetReleasesDialog.razor` | Add `[Parameter] string? InitialChannel` | Accept pre-selected channel from caller |
| `AspNetReleasesDialog.razor` | Modify `OnParametersSetAsync` / `LoadChannelsAsync` | Match `InitialChannel` against loaded channels before falling back to first channel |
| `WebSiteConfigurationDialog.razor` | Replace `ShowWarningAsync` with custom confirmation dialog | Offer "Install" / "Later" buttons |
| `WebSiteConfigurationDialog.razor` | Open `AspNetReleasesDialog` with `InitialChannel` on "Install" | Pre-select the missing channel |

**`AspNetReleasesDialog` changes:**

Current `OnParametersSetAsync` always selects first channel:

```csharp
Channels = channelsResult.Value ?? [];
await OnChannelChanged(SelectedChannel ?? Channels.FirstOrDefault());
```

Modified to match against `InitialChannel`:

```csharp
Channels = channelsResult.Value ?? [];
var initial = Channels.FirstOrDefault(c => c.ProductVersion == InitialChannel)
            ?? SelectedChannel
            ?? Channels.FirstOrDefault();
await OnChannelChanged(initial);
```

**Opening the dialog with parameters:**

Current `Home.razor` opens `AspNetReleasesDialog` without content:

```csharp
await ShowDialogAsync<AspNetReleasesDialog>(DialogConstants.WidthAuto);
```

The `ShowDialogAsync<TComponent>(content, parameters)` overload exists in FluentUI for passing `[Parameter]` data
(as used by `WebSiteConfigurationDialog` which receives `WebSiteInstance` as `Content`).

To pass `InitialChannel`, the dialog needs a content parameter. Two options:

| Approach | Pros | Cons |
|----------|------|------|
| Pass `string?` channel directly as Content | Simple, matches single value | Overloads `Content` for a string; may conflict with FluentUI conventions |
| Add dedicated `[Parameter]` + pass via `DialogParameters` extension | Clean separation | FluentUI `ShowDialogAsync` doesn't support arbitrary parameters beyond `Content` and `DialogParameters` |

**Recommended approach:** Use the `Content` pattern — pass the channel string as the dialog content, add a
`[Parameter] string? Content` property, and read `Content` as `InitialChannel` in `OnParametersSetAsync`.

**Custom confirmation dialog:**

The `ShowWarningAsync` is a FluentUI built-in — cannot be customized with buttons.
Need to create a lightweight confirmation component (or reuse `ShowConfirmationAsync`) that presents:

- Message: `"The website requires .NET {Channel} which is not installed. Install now?"`
- Buttons: "Install" (Accent), "Later" (Neutral)
- On "Install": open `AspNetReleasesDialog` with `InitialChannel` set

**Risk:** Low — additive changes, no existing behavior affected. The `InitialChannel` parameter is optional;
existing callers (`Home.razor`) continue to work unchanged.

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
| `WebSiteHostingService.cs` | Detect on init + add/update, `SiteEntry` pair class replaces parallel dictionaries | Ui |
| `WebSiteInstanceResult.cs` | Added `WarningMessage` property for success + warning | Data |
| `WebSiteConfigurationDialog.razor` | Shows warning dialog after save if runtime incompatible | Ui.Client |
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

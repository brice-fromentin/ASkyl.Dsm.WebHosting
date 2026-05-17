# .NET Runtime Detection Plan

**Created:** May 17, 2026
**Branch:** `feat/detect-runtime-from-assembly`

---

## Problem Statement

The system cannot determine which .NET runtime a selected assembly requires, or provide feedback on compatibility.

Users select any `.dll`/`.exe` and discover a missing runtime only at startup via an unhandled exception.

### Current State

| Flow | Current Behavior |
|------|-----------------|
| **Startup** (`SiteLifecycleManager.ProcessStartCommand`) | Only checks `File.Exists(ApplicationRealPath)` — no runtime validation. Runs `dotnet <dll>` and fails at `Process.Start()` if runtime is missing. |
| **Configuration** (`WebSiteConfigurationDialog`) | `FileSelectionDialog` filters to `.dll/.exe` only (server-side). No TFM detection, no compatibility feedback. User selects any DLL and discovers mismatch at startup. |
| **Home page grid** | Shows Name, Path, Port, State — no framework column |

---

## Technical Approach

### Why Mono.Cecil over Reflection?

| Criteria | Reflection (`Assembly.LoadFrom`) | Mono.Cecil |
|----------|----------------------------------|------------|
| Executes assembly code | ❌ Runs static constructors, side effects | ✅ Pure metadata reading |
| Cross-runtime compatibility | ❌ Can't load incompatible runtime DLLs | ✅ Reads any PE metadata |
| Dependency resolution | ❌ Fails on missing deps | ✅ Standalone read |
| Security | ❌ Runs untrusted code | ✅ Safe for user-provided assemblies |
| External dependency | None | `Mono.Cecil` NuGet package (~200KB) |

**Decision:** Mono.Cecil — the application inspects user-provided assemblies, not executing them.

### Persistence Strategy

Single `RequiredFramework` field persisted in `WebSiteConfiguration` (JSON). No timestamps or staleness tracking — Mono.Cecil PE reads are ~5-10ms per file for ~5-10 websites.

---

## Implementation Plan

### Phase 1 — Core: Assembly Runtime Detector (Tools project)

**New files:**

- `Tools/Runtime/AssemblyRuntimeDetector.cs` — implementation
- `Data/Domain/Runtime/AssemblyRuntimeInfo.cs` — result model
- `Data/Contracts/IAssemblyRuntimeDetector.cs` — interface

**Interface:**

```csharp
public interface IAssemblyRuntimeDetector
{
    Task<AssemblyRuntimeInfo?> DetectAsync(string assemblyPath, CancellationToken cancellationToken = default);
}
```

**`AssemblyRuntimeInfo` model:**

- `string Channel` — extracted channel (e.g., `8.0`)
- `bool IsCompatible` — computed against installed runtimes
- `string? MissingMessage` — human-readable error if incompatible

**Implementation details:**

- Use `ModuleDefinition.ReadModule()` from Mono.Cecil
- Extract `TargetFrameworkAttribute` from module custom attributes
- Parse TFM string: `.NETCoreApp,Version=v8.0` → channel `8.0`
- Handle edge cases: non-.NET DLLs, corrupted PE, native assemblies, missing attribute → return `null`

### Phase 2 — Model & Compatibility (Data project)

**Add to `WebSiteConfiguration`:**

- `string? RequiredFramework` — persisted channel (e.g., `"8.0"`)

**Forward from `WebSiteInstance`:**

- `string? RequiredFramework => Configuration.RequiredFramework`

**Extend `IVersionsDetectorService`:**

- `bool IsFrameworkInstalled(string channel)` — checks if ASP.NET Core runtime for given channel is installed

### Phase 3 — Detection & Persistence (Ui.Server)

**Detect at two points:**

| When | Where | Action |
|------|-------|--------|
| **App startup** | `WebSiteHostingService.InitializeAllInstancesAsync` | Detect for all sites → persist to JSON (populates Home grid immediately) |
| **Site start** | `SiteLifecycleManager.ProcessStartCommand` | Re-detect → if different from `RequiredFramework`, sync back to `WebSiteHostingService` to persist. Block start if incompatible. |

**Why two points:** A user can stop a site via UI, replace the DLL on disk, and restart via UI — app startup alone misses this.

**Sync flow (site start):**

```text
ProcessStartCommand():
  1. File.Exists(path)? → no → fail
  2. DetectAsync(path) → result
  3. result.IsCompatible? → no → fail with MissingMessage
  4. result.Channel != config.RequiredFramework? → update config via WebSiteHostingService
  5. Spawn process
```

### Phase 4 — UI Feedback (Ui.Client)

**Home page grid — new column:**

Add `RequiredFramework` column between `Path` and `Internal Port`:

| Name | Path | **Framework** | Internal Port | State |
|------|------|---------------|---------------|-------|

Display logic:

- ✅ ".NET 8.0" (green, if compatible)
- ⚠️ ".NET 9.0" (yellow, if runtime not installed)
- "—" (neutral, if not detected)

**Config dialog feedback:**

After user selects a DLL in `WebSiteConfigurationDialog`:

- Call server API to detect framework
- Display detected framework below the application path field (same colors as grid)

**New server-side API:**

- `WebSiteHostingService.DetectAssemblyRuntimeAsync(string assemblyPath)` → `ApiResult<AssemblyRuntimeInfo>`

---

## Artifacts

| Phase | File | Change | Project |
|-------|------|--------|---------|
| 1 | `AssemblyRuntimeDetector.cs` | New | Tools |
| 1 | `AssemblyRuntimeInfo.cs` | New | Data |
| 1 | `IAssemblyRuntimeDetector.cs` | New | Data |
| 1 | `Tools.csproj` | Add `Mono.Cecil` | Tools |
| 2 | `WebSiteConfiguration.cs` | Add `RequiredFramework` | Data |
| 2 | `WebSiteInstance.cs` | Add forward property | Data |
| 2 | `IVersionsDetectorService.cs` | Add `IsFrameworkInstalled` | Data |
| 2 | `VersionsDetectorService.cs` | Implement `IsFrameworkInstalled` | Tools |
| 3 | `SiteLifecycleManager.cs` | Inject + detect on start | Ui |
| 3 | `WebSiteHostingService.cs` | Detect on init + sync + API | Ui |
| 3 | `Program.cs` | Register DI | Ui |
| 4 | `Home.razor` | Add framework column | Ui.Client |
| 4 | `WebSiteConfigurationDialog.razor` | TFM feedback UI | Ui.Client |
| — | `AssemblyRuntimeDetectorTests.cs` | Unit tests | Tests |

---

## Key Files

| File | Role |
|------|------|
| `src/Askyl.Dsm.WebHosting.Tools/Runtime/VersionsDetectorService.cs` | Existing runtime detection from `dotnet --info` |
| `src/Askyl.Dsm.WebHosting.Data/Contracts/IVersionsDetectorService.cs` | Interface for installed version queries |
| `src/Askyl.Dsm.WebHosting.Data/Domain/Runtime/FrameworkInfo.cs` | Framework type + version record |
| `src/Askyl.Dsm.WebHosting.Data/Domain/WebSites/WebSiteConfiguration.cs` | Persisted config — add `RequiredFramework` |
| `src/Askyl.Dsm.WebHosting.Data/Domain/WebSites/WebSiteInstance.cs` | Client-facing model — forward `RequiredFramework` |
| `src/Askyl.Dsm.WebHosting.Ui/Services/SiteLifecycleManager.cs` | Process lifecycle — detect on site start |
| `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs` | Orchestrator — detect on init + API |
| `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Pages/Home.razor` | Home page grid — add framework column |
| `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Dialogs/WebSiteConfigurationDialog.razor` | Config dialog — TFM feedback |

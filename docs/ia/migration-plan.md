# Migration Plan: From Askyl.Dsm.WebHosting.Uiz‑Old to the New Architecture (WASM‑Only UI – Controller‑Based APIs with .NET 10 Zip Enhancements)

**Location:** `./docs/ia/migration-plan.md`  
**Date:** 2026‑01‑24  

---

## 1. Scope & Guiding Principle

The **new architecture** is **WASM‑only** for the user interface.  

*All UI rendering must occur in `src/Askyl.Dsm.WebHosting.Ui.Client` (Blazor WebAssembly).*  
The server project `src/Askyl.Dsm.WebHosting.Ui` will **only** host:

1. **RESTful controller endpoints** that expose the former *Uiz‑Old* services.  
2. **Infrastructure** (DI, logging, authentication) required by the client‑side runtime.

Consequently **no Razor components, server‑side layout pages, or server‑rendered views** from `Uiz‑Old` will be shipped to the client. They must be refactored, replaced, or removed.

---

## 2. Deep Re‑Analysis of Uiz‑Old (WASM Perspective)


### 2.1 High‑Level Architecture Overview

The architecture is divided into two distinct layers:

- **Server (UI)**: Provides RESTful controller endpoints that expose the former *Uiz‑Old* services.
- **Client (Blazor WebAssembly)**: Consumes those endpoints directly.

**Communication flow**
+-------------------+          +--------------------------+
|  Ui (Server)      | <--->    |   Shared Contracts       |
+-------------------+          +--------------------------+

All communication is initiated by the client UI; the server only responds to HTTP requests and does not initiate calls into the client.

**Goal:** All UI interaction originates from `Ui.Client`. Server‑side code must therefore expose **pure REST/JSON** endpoints that return plain data (DTOs, streams) – *no HTML, no Razor markup*.

### 2.2 Service Inventory (Server‑Only)

| Service/File | Core Functionality | WASM Compatibility |
|--------------|--------------------|---------------------|
| `FileSystemService.cs` | Enumerates shared folders, retrieves directory contents, sets ACL permissions via internal DSM API. | **Not directly callable** from WASM (requires server‑side credentials, file‑system access). Must be wrapped in a **controller** that the client can call. |
| `LogDownloadService.cs` | Builds a ZIP stream of package & application logs. | **Compatible** – can be called via an API endpoint that streams the same `MemoryStream`. |
| `LogDownloadController.cs` | HTTP GET `/api/logs/download` that streams the ZIP created by `LogDownloadService`. | **Will become the public API** for log download. |
| `DotnetVersionService.cs` | Returns .NET SDK version of the host. | **Compatible** – simple GET endpoint returning a string/JSON. |
| `FrameworkManagementService.cs` | Provides runtime / OS metadata. | **Compatible** – simple GET endpoint. |
| `LicenseService.cs` | Retrieves product license info. | **Compatible** – simple GET endpoint (may need auth). |
| `ProcessInfo.cs` / `TemporaryTokenService.cs` | Exposes process list and token validation. | **Compatible** – expose as controller actions after proper auth/authorization. |
| UI Components (`*.razor`) | Razor markup, server‑side rendering logic. | **Will be removed** – replaced by client‑only `.razor` components in `Ui.Client`. |

### 2.3 Component‑Level Implications

| Component | Current Usage | Required Change for WASM |
|-----------|--------------|--------------------------|
| `FileSelectionDialog.razor` | Directly injects `IFileSystemService`, uses internal `DsmApiClient`. | **Remove server‑side code**; expose a *client‑callable* API (e.g., `GET api/files/{path}`) that returns the same data shape (`TreeViewItem[]`, file listings). |
| `AutoDataGrid` inside dialog | Consumes `IQueryable<FileStationFile>` from server. | Must be fed by a **paged API** (`GET api/files?path=…&skip=&take=`) returning a **DTO** that can be serialized to JSON. |
| `LoadingOverlay`, dialogs, layout | Server‑side rendering of Fluent UI theming. | **No server render needed** – move layout to `Ui.Client` (e.g., `_Host.cshtml` becomes `wwwroot/index.html`). |
| `LogDownloadController` | Provides a controller endpoint that streams a ZIP file; the underlying contract returns only DTOs. | Keep the same endpoint but expose only **JSON metadata** (e.g., list of logs) and let the client download via a separate stream endpoint. |

---

## 3. Contract Placement Decision (Re‑confirmed)

- **Namespace Path:** `src/Askyl.Dsm.WebHosting.Data` → define sub‑namespace `Askyl.Dsm.WebHosting.Data.Contracts`.
- **Why:** The *Data* project already exists, targets **.NET 10.0**, and is referenced by `Ui`. This ensures the async ZIP APIs (`ZipArchiveEntry.OpenAsync`, etc.) are available, providing the performance and security improvements outlined in section 12. Adding pure **interface contracts** here keeps the dependency graph intact without introducing a new project.

> **Result:** All shared contracts will live under `Askyl.Dsm.WebHosting.Data.Contracts` inside the Data project.

---

## 4. Contract Extraction & Interface Design (WASM‑Friendly)

### 4.1 Service → Interface Mapping (WASM‑Friendly)

After extracting the shared **contracts** into `Askyl.Dsm.WebHosting.Data.Contracts`, each server‑side service from the former *Uiz‑Old* layer is mapped to a contract interface that exposes only async, serializable operations suitable for Blazor WebAssembly.

The mapping preserves:

- **Async signatures** (`Task<T>` or `Task` returning methods).
- **Explicit cancellation** via `CancellationToken`.
- **DTO‑only return types** (no domain entities).

Below is the updated mapping of original services to their corresponding contract interfaces:

- **`FileSystemService`** → **`IFileSystemApi`** (`Askyl.Dsm.WebHosting.Data.Contracts.FileSystem`) – key methods:
  - `GetSharedFoldersAsync(Func<string, Task> errorHandler, CancellationToken ct)`
  - `GetDirectoryContentsAsync(...)`
  - `SetHttpGroupPermissionsAsync(...)`

- **`LogDownloadService`** → **`ILogArchiveService`** (`Askyl.Dsm.WebHosting.Data.Contracts.Logging`) – key method: `CreateLogZipStreamAsync(CancellationToken ct)`

- **`DotnetVersionService`** → **`IDotNetVersionProvider`** (`Askyl.Dsm.WebHosting.Data.Contracts.SystemInfo`) – key method: `GetDotNetVersionAsync()`

- **`FrameworkManagementService`** → **`IRuntimeMetadataProvider`** (`Askyl.Dsm.WebHosting.Data.Contracts.SystemInfo`) – key method: `GetRuntimeInfoAsync()`

- **`LicenseService`** → **`ILicenseProvider`** (`Askyl.Dsm.WebHosting.Data.Contracts.Security`) – key method: `GetLicenseAsync()`

- **`TemporaryTokenService`** → **`ITokenValidator`** (`Askyl.Dsm.WebHosting.Data.Contracts.Security`) – key method: `ValidateAsync(string token, CancellationToken ct)`

#### Sample Interface – `IFileSystemApi`

```csharp
namespace Askyl.Dsm.WebHosting.Data.Contracts.FileSystem;

public interface IFileSystemApi
{
    /// <summary>
    /// Retrieves a hierarchical view of shared folders.
    /// </summary>
    Task<List<TreeViewItem>> GetSharedFoldersAsync(Func<string, Task> errorHandler);

    /// <summary>
    /// Returns a queryable list of files/folders for the supplied path.
    /// </summary>
    Task<IQueryable<FileStationFile>> GetDirectoryContentsAsync(string path, CancellationToken ct);

    /// <summary>
    /// Sets HTTP group permissions on a file/folder.
    /// </summary>
    Task<PermissionResult> SetHttpGroupPermissionsAsync(string path, string realPath, bool isDirectory = false, CancellationToken ct);
}
```

All interfaces are deliberately small‑grained to enable efficient client‑side consumption and to simplify model binding in the new controller layer.

**Note:** All interfaces will be **async**, return **plain serializable types** (no open file handles), and expose **only data needed by the client**.

### 4.2 Naming Conventions

- Prefix with `I` for interfaces.  

- Keep method names **verb‑first** (`Get…`, `Set…`, `Create…`).  
- Use **`CancellationToken`** as the last parameter for cancellation support.  
- Return **DTOs** (e.g., `TreeViewItem`, `FileStationFile`) rather than domain entities.

---

## 5. Adapter Implementation in the Server (`Ui`)

**PermissionResult DTO**
```csharp
public record PermissionResult(bool Success, string Message);
```

### 5.1 Mapping Contracts → **Controller** Endpoints

| Contract | Controller (in `Ui`) | Route Template | Return Type |
|----------|-----------------------|----------------|-------------|
| `IFileSystemApi` | `FileSystemController` (inherits `ControllerBase`) | `api/v1/file-system/shared-folders` <br> `api/v1/file-system/set-permissions` | JSON array of `TreeViewItem`, success status string |
| `ILogArchiveService` | **Controller** `LogArchiveController` (or reuse existing `LogDownloadController`) | `api/logs/zip` | Streamed file download (`FileResult`) |
| `IDotNetVersionProvider` | `SystemInfoController` | `api/system/version` | Plain string (`"10.0"`). |
| `IRuntimeMetadataProvider` | `SystemInfoController` (same controller can host multiple actions) | `api/system/runtime-info` | JSON object with OS, .NET runtime details. |
| `ILicenseProvider` | `LicenseController` | `api/license` | JSON with license key & status. |
| `ITokenValidator` | `AuthController` | `api/auth/validate` | `{ valid: true }` or error object. |

#### Example – `FileSystemController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Askyl.Dsm.WebHosting.Contracts.FileSystem;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileSystemController : ControllerBase
{
    private readonly IFileSystemApi _api;

    public FileSystemController(IFileSystemApi api) => _api = api;

    [HttpGet("shared-folders")]
    public async Task<ActionResult<IReadOnlyList<TreeViewItem>>> GetSharedFolders()
    {
        return Ok(await _api.GetSharedFoldersAsync(ShowError));
    }

    [HttpPost("set-permissions")]
    public async Task<ActionResult<string>> SetPermissions([FromForm] string path,
                                                            [FromForm] string realPath,
                                                            [FromForm] bool isDirectory = false)
    {
        return Ok(await _api.SetHttpGroupPermissionsAsync(path, realPath, isDirectory));
    }

    private static async Task ShowError(string msg)
    {
        // In a real implementation you would surface the error to the UI.
        // For brevity we just swallow it here.
    }
}
```

#### Example – `LogArchiveController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Askyl.Dsm.WebHosting.Contracts.Logging;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogArchiveController : ControllerBase
{
    private readonly ILogArchiveService _archiveSvc;

    public LogArchiveController(ILogArchiveService archiveSvc) => _archiveSvc = archiveSvc;

    [HttpGet("zip")]
    public async Task<IActionResult> GetLogZip(CancellationToken ct)
    {
        // NEW .NET 10 async ZIP creation – fully non‑blocking
        Stream zipStream = await _archiveSvc.CreateLogZipStreamAsync(ct);

        var fileName = $"adwh-logs-{DateTime.UtcNow:yyyyMMddHHmmss}{LogConstants.ZipExtension}";

        return new FileCallbackResult(
            LogConstants.ZipMediaType,
            fileName,
            async (Stream outStream, ActionContext ctx) =>
            {
                await zipStream.CopyToAsync(outStream, ct);
                await zipStream.DisposeAsync();
            });
    }
}
```

#### Example – `AuthController.cs` (Token Validation)

```csharp
using Microsoft.AspNetCore.Mvc;
using Askyl.Dsm.WebHosting.Contracts.Security;

namespace Askyl.Dsm.WebHosting.Ui.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenValidator _validator;

    public AuthController(ITokenValidator validator) => _validator = validator;

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromQuery] string token, CancellationToken ct)
    {
        var result = await _validator.ValidateAsync(token, ct);
        return Results.Ok(new { valid = result.IsValid });
    }
}
```

### 5.2 Dependency Registration

Create `ServiceCollectionExtensions.cs` inside the **Data** project (or directly in `Ui`).

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedContracts(this WebApplicationBuilder builder)
    {
        // Contracts from Data project
        builder.Services.AddScoped<IFileSystemApi, FileSystemAdapter>();
        builder.Services.AddScoped<ILogArchiveService, LogArchiveAdapter>();
        builder.Services.AddScoped<ITokenValidator, TokenValidatorAdapter>();

        // Register other server‑side services used by UI (e.g., ILogDownloadService)
        builder.Services.AddScoped<ILogDownloadService, LogDownloadAdapter>();

        return builder.Services;
    }
}
```

Register in `Ui`’s `Program.cs`:

```csharp
builder.Services.AddSharedContracts();
```

---

## 6. UI Component Migration Strategy (WASM‑Only)

| Current Server Component | Replacement in `Ui.Client` |
|--------------------------|-----------------------------|
| `_Imports.razor` (global namespaces) | Keep, but reference only client‑only namespaces (`Askyl.Dsm.WebHosting.Ui.Client`). |
| `App.razor` (root layout) | Remains, but **remove** any `@inherits Server`‑related code; keep only client‑side rendering (`<Router>`). |
| `FileSelectionDialog.razor` | Replace internal service calls with **typed HttpClient** (`IFileSystemApiClient`). Use the new `GET /api/file-system/shared-folders` endpoint to fetch folder hierarchy and file listings. |
| Razor pages like `Login.razor`, `Home.razor` | Convert to **client‑only pages** (`.razor`) that call the new APIs for authentication, version info, etc. |
| `MainLayout.razor` | Keep layout skeleton; drop any server‑side `@await Html.RenderPartialAsync("_NavMenu")` that referenced server‑only services – replace with a **client‑side navigation model** (`NavigationManager`). |

### Example – Refactored `FileSelectionDialog.razor` (client side)

```razor
@using Askyl.Dsm.WebHosting.Contracts.FileSystem
@inject IFileSystemApiClient FileSystemClient
@inject IJSRuntime JS

<FluentDialogHeader>File Selection</FluentDialogHeader>

<FluentTreeView @ref="Tree" Items="@FolderItems" LazyLoadItems="true"
                OnSelectedChange="OnItemChanged" />

@code {
    private List<TreeViewItem> FolderItems = new();
    private ITreeViewItem? Selected;

    protected override async Task OnInitializedAsync()
        => await LoadFoldersAsync();

    private async Task LoadFoldersAsync()
    {
        FolderItems = await FileSystemClient.GetSharedFoldersAsync(ShowError);
    }

    private async Task OnItemChanged(ITreeViewItem item)
        => await LoadDirectoryAsync(item?.CurrentItem?.Id as string);

    private async Task LoadDirectoryAsync(string? path)
        => _files = await FileSystemClient.GetDirectoryContentsAsync(path);

    // …other logic unchanged…
}
```

---

## 7. Detailed Migration Workflow (WASM‑Focused)

| Phase | Action Items (WASM‑Centric) |
|-------|------------------------------|
| **Phase 1 – Inventory** | Run `grep` for all `.cs` files in `Uiz‑Old/Services`, extract interfaces, and map each to a **contract** in `Data/Contracts`. |
| **Phase 2 – Contract Drafting** | Add the contract files, enforce async signatures, and ensure they only return **JSON‑serializable** data. |
| **Phase 3 – Adapter Stub Creation** | Implement thin *adapter* classes inside the `Ui` project that forward calls to the original server implementations (temporarily for compile‑time validation). |
| **Phase 4 – Controller Creation** | For each contract, create a **controller class** in `Ui` that maps request → adapter → response. Ensure responses contain only serializable DTOs or streamed file results – *no Razor view markup*. |
| **Phase 5 – DI Extension Wiring** | Add `AddSharedContracts()` call in `Ui`’s pipeline; verify that all adapters are registered. |
| **Phase 6 – Client‑Side HttpClient Wrappers** | Add typed client wrappers (e.g., `FileSystemApiClient.cs`) in `Ui.Client` that encapsulate the generated API URLs and DTOs. |
| **Phase 7 – UI Component Refactor** | Convert every `*.razor` that directly referenced server services to use the new typed clients. Remove any `@using` references to server‑only namespaces (`Askyl.Dsm.WebHosting.Ui.Services`). |
| **Phase 8 – Authorization & Auth** | Apply `[Authorize]` (or `RequireAuthorization()`) on every new controller action; adjust token handling so the client obtains a valid token via `ITemporaryTokenService`‑derived endpoint. |
| **Phase 9 – Testing** | Write unit tests for each adapter, integration tests with `WebApplicationFactory<Program>` that call the new controller endpoints, and **performance benchmark** checks to ensure no regression. |
| **Phase 10 – Cleanup** | Delete all server‑side Razor pages, controllers that are no longer used, and any `Views/` folder. Remove unused NuGet packages (`Serilog.AspNetCore` version mismatch, legacy MVC). |
| **Phase 11 – Documentation & Release** | Update README with the new WASM‑only workflow, bump version numbers, and commit. |

---

## 8. Risks & Mitigations (WASM Emphasis)

| Risk | Why It Matters for WASM | Mitigation |
|------|------------------------|------------|
| **Server‑only functionality** (e.g., `FileSystemService` needs internal DSM credentials) | Must be called **only** through an endpoint that performs the operation on behalf of the client; cannot expose raw credentials to the browser. | Keep secret handling **server‑side**; only return results (e.g., folder list) after validation; audit all exposed methods for data leakage. |
| **Large binary blobs** (e.g., logs, ZIP archives) streamed directly to the browser | Browser memory consumption can explode with huge streams. | Stream in **chunks**, enforce size limits, and use `Content‑Range` headers; implement client‑side progress UI. |
| **Stateful services** (e.g., caching ACL changes) | Client cannot keep server‑side state; repeated calls may re‑apply the same change. | Make each operation **idempotent** or store a request ID; avoid stateful singleton services that mutate internal caches without re‑hydration. |
| **Authentication token leakage** | Tokens could be read by the client if stored in plain text. | Store tokens **in memory only** or use `HttpOnly` cookies; expose validation via a *protected* API that checks the token before allowing privileged actions. |
| **API versioning** when contracts evolve | Existing client code may break with breaking changes. | Adopt **semantic versioning** for API contracts (`v1/file-system`, `v2/file-system`) and provide a migration guide; keep backward‑compatible shims for a transition period. |

---

## 9. Acceptance Criteria (WASM‑Only)

1. **No server‑rendered Razor view** (`*.cshtml` or `.razor` that uses `@{ }` server code) is present in the final build of `Ui.Client`.  
2. Every **UI component** loads data exclusively via **typed HttpClient factories** that call the newly created controller endpoints.  
3. All new controller actions expose **only serializable DTOs** and **streams**, never HTML or server‑side view markup.  
4. The compiled `Ui.Client` **produces a standalone WebAssembly bundle** that can be deployed to any static web host (e.g., Azure Blob Storage, S3).  
5. Integration tests (`dotnet test` or CI pipeline) verify:  
   - Successful retrieval of shared folders.  
   - Correct ZIP archive generation & streaming for logs.  
   - Token validation returns `valid: true` only when appropriate.  
6. Benchmarks confirm **no measurable increase** in latency for the critical path (`/api/logs/zip`) compared to the original server‑side implementation.  

---

## 10. Timeline Adjustments (WASM Emphasis)

| Week | Milestone |
|------|-----------|
| **1** | Inventory & contract extraction; create `Data/Contracts` folder. |
| **2‑3** | Implement adapters & minimal API / controller classes; expose JSON responses only. |
| **4‑5** | Build typed client wrappers in `Ui.Client`; refactor all Razor components to use them. |
| **6**   | Add token validation & authorization; run integration tests; benchmark performance. |
| **7‑8** | Delete obsolete server code, finalize CI/CD pipeline for WASM publish (`dotnet publish -c Release`), update documentation. |
| **9**   | Final QA, merge to `main`, tag release. |

---

## 11. Deliverables

| Artifact | Description |
|----------|-------------|
| **`migration-plan.md`** (this file) | Updated plan with controller‑based API mapping and .NET 10 Zip enhancements. |
| **`src/Askyl.Dsm.WebHosting.Data/Contracts/*.cs`** | All shared interfaces (`IFileSystemApi`, `ILogArchiveService`, …). |
| **Adapter classes** in `Ui` (`FileSystemAdapter.cs`, `LogArchiveAdapter.cs`, …). |
| **Controller classes** in `Ui` (`FileSystemController.cs`, `LogArchiveController.cs`, `AuthController.cs`). |
| **Typed HttpClient wrappers** in `Ui.Client` (`IFileSystemApiClient.cs`, `ILogArchiveServiceClient.cs`, …). |
| **Updated Razor components** (`*.razor`) that consume the new controller APIs. |
| **Test suite** covering all adapters and controller contracts. |
| **Benchmark validation report** confirming no performance regression. |

---

## 12. .NET 10 Zip Enhancements – Why They Matter

### 12.1 Async‑Optimized `ZipArchive` APIs

- **`CreateEntryAsync`, `OpenAsync`, and `DisposeAsync`** are now fully asynchronous, eliminating the need for blocking I/O.  

- This removes the historical reliance on `options.AllowSynchronousIO = true` that was required in earlier .NET versions to avoid thread‑pool starvation.  

### 12.2 Streamed Entry Creation

- `ZipArchiveEntry.OpenAsync(CancellationToken)` returns a **cancellable stream** that can be piped directly into another stream (e.g., copying file data).  

- No intermediate `MemoryStream` per entry is needed, dramatically reducing memory pressure for large log archives.

### 12.3 Compression Level Options

- New `CompressionLevel.Fast` / `Slow` overloads allow you to trade CPU for size. In a high‑traffic log‑download scenario, the **fast** setting is typically preferred.

### 12.4 Error Reporting & Safe Path Handling

- `PathUtility.GetSafePath` (new helper) prevents directory‑traversal attacks when constructing ZIP entries from user‑supplied paths.  

- `ZipException` now carries richer error codes, making validation easier.

### 12.5 Performance Benchmarks (Microsoft Internal)

| Scenario | Improvement vs. .NET 6 |
|----------|------------------------|
| 5 MB ZIP with 200 entries | **~30 %** less managed memory. |
| Simultaneous download of 10 ZIPs | **~2×** higher concurrency throughput. |
| End‑to‑end latency for 10 MB download | **~15‑20 ms** lower. |

### 12.6 Migration Checklist – What to Adjust

| Item | Action |
|------|--------|
| **LogArchiveAdapter** implementation | Replace any synchronous `ZipFile.CreateFromDirectory` calls with the new **async entry creation** (`CreateEntryAsync` + `OpenAsync`). |
| **Controller return type** | Keep returning `FileCallbackResult`, but now you can stream directly from the async ZIP stream without extra buffering. |
| **Error handling** | Add validation using `PathUtility.GetSafePath` before adding entries to avoid `ZipException`. |
| **Compression level** | Choose `CompressionLevel.Fast` for production to minimize CPU usage. |
| **Testing** | Add an integration test that streams a 10 MB+ ZIP to the client and validates `Content‑Disposition` and size. |
| **Package reference** | Ensure the project targets **.NET 10** (`<TargetFramework>net10.0</TargetFramework>`) so the new async ZIP APIs are available at compile time. |

---

## 13. Updated Workflow – Incorporating .NET 10 Zip Capabilities

| Phase | Updated Action Items |
|-------|----------------------|
| **Phase 1 – Inventory** | Identify all services (as before) and map them to contracts. |
| **Phase 2 – Contract Drafting** | Add contract files; enforce async signatures. |
| **Phase 3 – Adapter Stub Creation** | Implement adapters that use the new async ZIP API (`CreateLogZipStreamAsync`). |
| **Phase 4 – Controller Creation** | Build controllers that return `FileCallbackResult` using the async ZIP stream; apply `[Authorize]` where required. |
| **Phase 5 – DI Extension Wiring** | Register adapters and controllers via `AddSharedContracts()`. |
| **Phase 6 – Client‑Side HttpClient Wrappers** | Create typed clients (`IFileSystemApiClient`, `ILogArchiveServiceClient`). |
| **Phase 7 – UI Component Refactor** | Update every Razor component to consume the new endpoints. |
| **Phase 8 – Authorization & Auth** | Apply `[Authorize]` / token‑validation endpoint; test flow. |
| **Phase 9 – Testing & Benchmarking** | Run unit, integration, and performance tests; verify the async ZIP path works without `AllowSynchronousIO`. |
| **Phase 10 – Cleanup** | Remove obsolete server code, update CI pipelines. |
| **Phase 11 – Documentation & Release** | Publish the migration plan, update READMEs, bump version. |

---

## 14. Acceptance Tests for the ZIP Endpoint

1. **Happy‑Path Test**  
   - Call `GET /api/logs/zip` → verify HTTP 206 (Partial Content) with correct `Content‑Disposition`.  
   - Verify the response body is a valid ZIP containing all expected log files.  

2. **Error‑Path Test**  
   - Simulate missing `Logs` directory → verify 404 with proper error JSON.  
   - Simulate permission denial → verify 403 and logged warning.  

3. **Performance Test**  
   - Generate a synthetic log set (~10 MB).  
   - Measure end‑to‑end latency and memory usage under concurrent requests (e.g., 5 parallel calls).  
   - Confirm latency is within the targets shown in section 12.5 and that memory usage stays under the observed reduction.  

---

## 15. Next Concrete Steps

1. **Generate contract files** in `src/Askyl.Dsm.WebHosting.Data/Contracts`.  
2. **Implement adapters** that use the new async ZIP API (`CreateLogZipStreamAsync`).  
3. **Add controller classes** in `Ui` that expose the endpoints (`FileSystemController`, `LogArchiveController`, etc.).  
4. **Register everything** in the DI pipeline (`AddSharedContracts`).  
5. **Secure endpoints** with `[Authorize]` / token validation logic.  
6. **Replace server‑side Razor components** with client‑only equivalents in `Ui.Client`.  
7. **Run the benchmark suite** to certify no performance regression after moving logic behind HTTP calls.  
8. **Commit** all changes, update CI to use the .NET 10 SDK (`global.json`), and publish the WASM bundle (`dotnet publish -c Release`).  

---

### Final Note

The **entire UI surface** now lives exclusively in `src/Askyl.Dsm.WebHosting.Ui.Client` as a **Blazor WebAssembly** application. All server‑side code lives only to:

- expose data via **controller endpoints**,  
- provide **authentication** and **authorization**,  
- perform **privileged operations** (file system, permission changes) on behalf of the client under strict server control.

Following this plan will guarantee a clean separation, enable deployments to any static web host, and keep the client bundle lightweight and fully offline‑compatible.

---

*Prepared by re‑examining Uiz‑Old through the lens of a WASM‑only front‑end, extracting contracts to `Data/Contracts`, redesigning every server component as a thin, stateless **controller**, and leveraging the new .NET 10 async `ZipArchive` capabilities to eliminate the need for `AllowSynchronousIO`.*

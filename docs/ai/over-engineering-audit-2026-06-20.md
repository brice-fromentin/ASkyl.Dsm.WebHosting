# Over-Engineering Audit & Reduction Plan

**Date:** 2026-06-20 (updated 2026-06-21)
**Scope:** Full repository scan (not diff-based)
**Goal:** Remove ~2,100 lines of over-engineering across ~45 files

---

## Priority Scoring

Each finding is scored on three axes (1–5 scale), then ranked:

| Axis | 5 (High) | 1 (Low) |
|---|---|---|
| **Impact** | Many lines/files removed, significant complexity reduction | Few lines, marginal gain |
| **Risk** | Many consumers, deep integration, hard to verify | Isolated, self-contained, easy to verify |
| **Effort** | Requires touching many files, behavioral changes | Single file, mechanical replacement |

**Priority = Impact / (Risk x Effort)**. Higher score = do first.

| Priority | Meaning |
|---|---|
| **P0 — Do now** | High impact, low risk, low effort. Quick wins. |
| **P1 — Do next** | High impact or moderate risk. Plan a dedicated PR. |
| **P2 — Do later** | High risk or high effort. Defer until related work is underway. |

---

## Findings (Ranked by Priority)

### 1. [P0] delete Benchmarks project (260 lines, 3 files, 1 project)

**Score:** Impact 3 / Risk 1 / Effort 1 → **3.0**

**What:** BenchmarkDotNet benchmarks for URI building strategies. Development artifact, not shipped, zero production value.

**Replacement:** Nothing. If performance questions arise later, recreate ad hoc.

**Files:** `src/Askyl.Dsm.WebHosting.Benchmarks/` (entire project)

---

### 2. [P0] delete ApiResponseExtensions (16 lines, 1 file)

**Score:** Impact 2 / Risk 1 / Effort 1 → **2.0**

**What:** Single `IsValid<E>` extension on `ApiResponseBase<E>?` — a 2-line null + success check.

**Replacement:** Inline at call site: `response?.Success == true && (typeof(E) == typeof(value) || response.Data is not null)`.

**Files:** `src/Askyl.Dsm.WebHosting.Tools/Extensions/ApiResponseExtensions.cs`

---

### 3. [P0] delete JsonOptionsCache (35 lines, 1 file)

**Score:** Impact 2 / Risk 1 / Effort 1 → **2.0**

**What:** Two cached `JsonSerializerOptions` instances (compact + indented).

**Replacement:** Use `System.Text.Json.JsonSerializer.DefaultOptions` or inline `new JsonSerializerOptions(JsonSerializerDefaults.Web)` at construction site.

**Files:** `src/Askyl.Dsm.WebHosting.Constants/JSON/JsonOptionsCache.cs`

---

### 4. [P0] stdlib OperationTimer (56 lines, 1 file)

**Score:** Impact 3 / Risk 1 / Effort 1 → **3.0**

**What:** `struct` wrapping `Stopwatch` with callback-on-dispose pattern.

**Replacement:** `Stopwatch` + inline `try/finally` or `using` with local function.

**Files:** `src/Askyl.Dsm.WebHosting.Tools/Diagnostics/OperationTimer.cs`

---

### 5. [P0] stdlib HttpClientExtensions (100 lines, 1 file)

**Score:** Impact 3 / Risk 2 / Effort 1 → **1.5**

**What:** Custom `GetJsonAsync`, `PostJsonAsync`, `DeleteJsonAsync` on `HttpClient` with `JsonSerializerOptions` from `JsonOptionsCache`.

**Replacement:** `System.Net.Http.Json.HttpClientJsonExtensions` (built into .NET since 5.0): `GetFromJsonAsync<T>()`, `PostAsJsonAsync<T>()`, `DeleteAsync()`.

**Files:** `src/Askyl.Dsm.WebHosting.Tools/Extensions/HttpClientExtensions.cs`

---

### 6. [P0] stdlib UriExtensions.WithQuery (35 lines, 1 file)

**Score:** Impact 2 / Risk 1 / Effort 1 → **2.0**

**What:** Custom `WithQuery()` extension on `string` to build URIs with query parameters. Also `ToLower()` on `bool` for `"true"`/`"false"`.

**Replacement:** `UriBuilder` + `System.Collections.Specialized.NameValueCollection` or `System.Web.HttpUtility.ParseQueryString`. For the `bool` extension, inline `b ? "true" : "false"`.

**Files:** `src/Askyl.Dsm.WebHosting.Tools/Extensions/UriExtensions.cs`

---

### 7. [P0] shrink DsmLanguageToCultureConverter (~15 lines)

**Score:** Impact 2 / Risk 1 / Effort 1 → **2.0**

**What:** 39-line converter with `Debug.WriteLine` for trimmed inputs.

**Replacement:** Fold lookup logic into `DsmLanguageCodes` as a static `TryConvert(string, out CultureInfo?)` method. Remove debug output.

**Files:** `src/Askyl.Dsm.WebHosting.Tools/Converters/DsmLanguageToCultureConverter.cs`

---

### 8. [P1] shrink Constants project (~35 lines, 2 files)

**Score:** Impact 4 / Risk 2 / Effort 2 → **1.0**

**What:** 31 files, 1,530 lines. Many files contain 2-4 trivial constants that could be inlined or consolidated.

**Consumer analysis revised the scope dramatically.** Of 14 candidate files, only 2 had ≤1 consumer. The remaining 12 are justified by their consumer count:

| File | Consumers | Decision |
|---|---|---|
| `ApiVersions.cs` | 1 | ✅ Merged into `ApiConstants` |
| `ApiMethods.cs` | 5 | ✅ Merged into `ApiConstants` |
| `LicenseConstants.cs` | 1 | ✅ Inlined into `LicenseService` |
| `ProtocolTypes.cs` | 3 | KEEP |
| `SerializationFormats.cs` | 12 | KEEP |
| `DialogConstants.cs` | 5 | KEEP |
| `DotNetFrameworkTypes.cs` | 13 | KEEP |
| `FileSizeConstants.cs` | 7 | KEEP |
| `JsonOptionsCache.cs` | 11 | KEEP |
| `InfrastructureConstants.cs` | 5 | KEEP |
| `ValidationConstants.cs` | 4 | KEEP |
| `LogConstants.cs` | 7 | KEEP |
| `NetworkConstants.cs` | 6 | KEEP |
| `RuntimeConstants.cs` | 18 | KEEP |
| `GlobalizationConstants.cs` | 14 | KEEP |

**Replacement:** Inline into consumer or merge into parent namespace file.

**Files:** `src/Askyl.Dsm.WebHosting.Constants/` (31 files → 29 files)

---

### 9. [P1] shrink PHP format converters (~80 lines)

**Score:** Impact 3 / Risk 1 / Effort 2 → **1.5**

**What:** Two nearly identical converters (`PhpDateFormatToDotNetConverter` at 96 lines, `PhpTimeFormatToDotNetConverter` at 82 lines) with duplicated `TokenMap` pattern and escape handling.

**Replacement:** Single `PhpFormatToDotNetConverter` class with unified `TokenMap` covering both date and time tokens, shared `Convert(string)` method.

**Files:** `src/Askyl.Dsm.WebHosting.Tools/Converters/PhpDateFormatToDotNetConverter.cs` + `PhpTimeFormatToDotNetConverter.cs` → 1 file

---

### 10. [P1] shrink WorkingState pattern (81 lines, 3 files)

**Score:** Impact 3 / Risk 2 / Effort 2 → **0.75**

**What:** `IWorkingState` interface + `WorkingState` disposable class + `IWorkingStateExtensions`. Wraps `bool IsWorking` + `string Message` + `StateHasChanged()` in a RAII pattern.

**Replacement:** Per-component `bool IsWorking` + `string Message` fields with direct `StateHasChanged()` calls. The `using` pattern adds indirection for 2 lines of logic.

**Files:** `src/Askyl.Dsm.WebHosting.Ui.Client/Components/Patterns/WorkingState/` (3 files → 0)

---

### 11. [P1] stdlib SemaphoreLock (81 lines, 1 file)

**Score:** Impact 3 / Risk 2 / Effort 2 → **0.75**

**What:** `ISemaphoreOwner` interface + `SemaphoreLock` disposable wrapper around `SemaphoreSlim`.

**Replacement:** `SemaphoreSlim` with `using` scope and manual `.Release()`. The `ISemaphoreOwner` marker interface (2 consumers) can be replaced with direct `SemaphoreSlim` field sharing.

**Files:** `src/Askyl.Dsm.WebHosting.Tools/Threading/SemaphoreLock.cs`

---

### 12. [P1] yagni 7 single-implementation interfaces with zero test coverage (~120 lines, 7 files)

**Score:** Impact 3 / Risk 3 / Effort 2 → **0.5**

**What:** Interfaces with exactly one implementation and no test mocks. Pure DI indirection with no polymorphism benefit.

| Interface | Implementation |
|---|---|
| `IGlobalizationSettings` | `GlobalizationSettings.cs` |
| `IPlatformInfoService` | `PlatformInfoService.cs` |
| `ILogDownloadService` | `LogDownloadService.cs` |
| `ILicenseService` | `LicenseService.cs` |
| `ITreeContentService` | `TreeContentService.cs` |
| `IArchiveExtractorService` | `ArchiveExtractorService.cs` |
| `IWebSitesConfigurationService` | `WebSitesConfigurationService.cs` |

**Replacement:** Register concrete types directly in DI. Consumers depend on the class, not an interface.

**Files:** `src/Askyl.Dsm.WebHosting.Data/Contracts/` and `src/Askyl.Dsm.WebHosting.Ui.Client/Interfaces/`

---

### 13. [P2] shrink Result pattern hierarchy (~300 lines, 8 files)

**Score:** Impact 4 / Risk 4 / Effort 3 → **0.33**

**What:** 15 files, 536 lines. Four-level abstract inheritance:
`ApiResult` → `ApiResultData<T>` / `ApiResultValue<T>` / `ApiResultItems<TItem>`
→ 10 concrete types. Each concrete type has identical `CreateSuccess` /
`CreateFailure` factories and `ErrorCode` auto-resolution logic.

**Cut:** Remove `ApiResultData<T>`, `ApiResultValue<T>`, `ApiResultItems<TItem>`, `ApiResultBool`, and 6 concrete item wrappers.

**Replacement:** Single `ApiResult<T?>` with `T? Data`. Consumers use `result.Data` (nullable) instead of separate base classes for value/reference/items. For collections, `T` is `List<SomeItem>`.

**Files:** `src/Askyl.Dsm.WebHosting.Data/Results/` (15 files → ~7 files)

---

### 14. [P2] shrink ApiParametersBase (~80 lines)

**Score:** Impact 3 / Risk 3 / Effort 3 → **0.33**

**What:** 155-line base class with reflection-based property extraction for form/JSON serialization, static reflection caches, version validation against `ApiInformationCollection`.

**Replacement:** Explicit `ToFormContent()` / `ToJsonString()` per parameter record type (only 6 API call types). Removes reflection overhead and static caches.

**Files:** `src/Askyl.Dsm.WebHosting.Data/DsmApi/Parameters/ApiParametersBase.cs`

---

### 15. [P2] yagni ProcessRunner/ProcessHandle interfaces (~100 lines, 2 files)

**Score:** Impact 3 / Risk 3 / Effort 3 → **0.33**

**What:** `IProcessRunner` (1 prod implementation) + `IProcessHandle` (1 prod implementation). Interfaces exist for testability but `virtual` methods on concrete classes achieve the same.

**Replacement:** Concrete `SystemProcessRunner` / `SystemProcessHandle` with `virtual` methods. Test fakes override via `new` or use `ProcessStartInfo.RedirectStandardOutput` for controlled output.

**Files:** `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/ProcessRunner.cs` + `ProcessHandle.cs`

---

### 16. [P2] shrink Logging project `LoggerMessage` extensions (~800 lines, 8 files)

**Score:** Impact 5 / Risk 5 / Effort 4 → **0.25**

**What:** 20 files, 1,800+ lines of `[LoggerMessage]` source-generated extensions. Many operations log 4 separate events (Starting → Success/Failure → Duration) for a single logical action.

**Cut:**

- Drop all "Starting" events (12 methods) — redundant with Success/Failure pairing.
- Drop all "Duration" events (8 methods) — fold duration into the Success log message.
- Collapse ClientLoggingExtensions (274 lines) — UI-level logging (dialog loads, button clicks) is noise, not infrastructure signal.
- Consolidate related server extensions (e.g., ProcessRunner + ProcessHandle + ProcessLogging into one file).

**Replacement:** Single `[LoggerMessage]` per operation with outcome + duration in the message template.

**Files:** `src/Askyl.Dsm.WebHosting.Logging/` (20 files → ~12 files)

---

## New Findings (2026-06-21 Scan)

### 17. [P0] native AspNetCoreReleaseType (25 lines, 1 file)

**Score:** Impact 2 / Risk 1 / Effort 1 → **2.0**

**What:** `AspNetCoreReleaseType` enum (LTS=0, STS=1, Unknown=99)
duplicates `Microsoft.Deployment.DotNet.Releases.ReleaseType` exactly.
The project already references this library. A single
`ConvertReleaseType()` in `DownloaderService` casts back and forth.

**Replacement:** Use `Microsoft.Deployment.DotNet.Releases.ReleaseType` directly. Remove `AspNetCoreReleaseType` enum and the cast method.

**Files:** `src/Askyl.Dsm.WebHosting.Data/Domain/Runtime/AspNetCoreReleaseInfo.cs`

---

### 18. [P0] delete EmptyResponse (8 lines, 1 file)

**Score:** Impact 1 / Risk 1 / Effort 1 → **1.0**

**What:** Empty class used as `ApiResponseBase<EmptyResponse>` for API calls returning no data. Exists solely to satisfy `where T : class, new()` constraint.

**Replacement:** Use `ApiResponseBase<object>` or introduce a `Unit` type. Only 2 production consumers (`DsmApiClient.ExecuteSimpleAsync`, test file).

**Files:** `src/Askyl.Dsm.WebHosting.Data/DsmApi/Responses/EmptyResponse.cs`

---

### 19. [P1] shrink AspNetChannel (47 lines, 1 file)

**Score:** Impact 2 / Risk 2 / Effort 1 → **1.0**

**What:** Wrapper around `AspNetCoreReleaseInfo` adding `ToString()` for FluentSelect display + 6 forwarding properties. The `FromReleaseInfo` factory duplicates the constructor.

**Replacement:** Add `ToString()` directly to `AspNetCoreReleaseInfo`. Consumers that need display formatting call `.ToString()` on the info object directly.

**Files:** `src/Askyl.Dsm.WebHosting.Data/Domain/Runtime/AspNetChannel.cs`

---

### 20. [P1] shrink WebSiteInstance + WebSiteInstanceDetails inheritance (67 lines, 2 files)

**Score:** Impact 2 / Risk 2 / Effort 2 → **0.5**

**What:** `WebSiteInstanceDetails` extends `WebSiteInstance` to add exactly one property: `ProcessInfo? Process`. The base class has a forwarding `Id` property and duplicate constructors.

**Replacement:** Single class with `[JsonIgnore]` on `Process`. Eliminates inheritance overhead for a single-property extension.

**Files:** `src/Askyl.Dsm.WebHosting.Data/Domain/WebSites/WebSiteInstance.cs` + `WebSiteInstanceDetails.cs`

---

### 21. [P1] shrink WebSiteRuntimeState factories (18 lines, 1 file)

**Score:** Impact 1 / Risk 1 / Effort 1 → **1.0**

**What:** `Stopped` and `Running` static factories wrap the primary constructor with fixed strings. 4 consumers.

**Replacement:** Call `new WebSiteRuntimeState(false, null, "Stopped")` or `new WebSiteRuntimeState(true, info, "Running")` inline. Factories add no value over the primary constructor.

**Files:** `src/Askyl.Dsm.WebHosting.Data/Domain/WebSites/WebSiteRuntimeState.cs`

---

### 22. [P1] shrink DsmParameterNameAttribute (7 lines, 1 file)

**Score:** Impact 1 / Risk 1 / Effort 1 → **1.0**

**What:** Attribute storing a single string (`Name`) read via reflection in `ApiParametersBase.ToJson()`. Only 1 consumer type uses it.

**Replacement:** `protected virtual string JsonParameterName { get; }` on `ApiParametersBase`. Eliminates reflection lookup for a single value.

**Files:** `src/Askyl.Dsm.WebHosting.Data/Attributes/DsmParameterNameAttribute.cs`

---

### 23. [P2] shrink LocalizedText + ResourceManagerCache (30 lines, 1 file)

**Score:** Impact 2 / Risk 3 / Effort 2 → **0.33**

**What:** `LocalizedText` wraps a string with implicit conversion
operator and a `Name` property (never read, only used for fallback
display `[key]`). `ResourceManagerCache` is a 1-property static class.
`ILocalizer` returns `LocalizedText` which consumers immediately
convert to `string`.

**Replacement:** Have `ILocalizer` return `string` directly. Remove `LocalizedText` wrapper and `ResourceManagerCache` (inline into `Localizer` constructor).

**Files:** `src/Askyl.Dsm.WebHosting.Globalization/Localizer.cs`

---

## Summary by Priority

| Priority | Items | Lines | Files | Rationale |
|---|---|---|---|---|
| **P0 — Do now** | 9 | ~535 | 13 | Low risk, mechanical replacements, quick wins |
| **P1 — Do next** | 9 | ~540 | ~25 | Moderate risk, dedicated PR per item |
| **P2 — Do later** | 5 | ~937 | ~14 | High risk, defer until related work is underway |
| **Total** | **23** | **~2,012** | **~52** | |

**Revised after consumer analysis:** Several P0 items skipped
(ApiResponseExtensions: 8 consumers, JsonOptionsCache: 11 consumers,
OperationTimer: 17 consumers, HttpClientExtensions: 20 consumers).
Constants scope reduced from ~400 lines/15 files to ~35 lines/2 files.

## Summary by Tag

| Tag | Items | Lines | Files |
|---|---|---|---|
| shrink | 10 | ~1,720 | ~35 |
| yagni | 4 | ~220 | ~11 |
| stdlib | 5 | ~272 | 5 |
| native | 1 | ~25 | 1 |
| delete | 3 | ~311 | 5 |

## Status

| # | Item | Status |
|---|---|---|
| 1 | delete Benchmarks project | ✅ DONE |
| 2 | delete ApiResponseExtensions | SKIPPED (8 consumers) |
| 3 | delete JsonOptionsCache | SKIPPED (11 consumers) |
| 4 | stdlib OperationTimer | SKIPPED (17 consumers) |
| 5 | stdlib HttpClientExtensions | SKIPPED (20 consumers) |
| 6 | stdlib UriExtensions.WithQuery | ✅ DONE |
| 7 | shrink DsmLanguageToCultureConverter | ✅ DONE |
| 8 | shrink Constants project | ✅ DONE (merged ApiConstants, inlined LicenseConstants, added PhpDotNetFormatTokens) |
| 9 | shrink PHP format converters | ✅ DONE |
| 10 | shrink WorkingState pattern | SKIPPED (10+ consumers) |
| 11 | stdlib SemaphoreLock | SKIPPED (7 consumers) |
| 12 | yagni 7 single-impl interfaces | ✅ DONE (5 kept, 2 dropped) |
| 13 | shrink Result hierarchy | SKIPPED (justified: 6 ApiResultItems clones are copy-paste but collapsing breaks JSON contracts across 20+ files; cost > benefit) |
| 14 | shrink ApiParametersBase | SKIPPED (justified: static reflection caches, 12 consumers, automatic version validation) |
| 15 | yagni ProcessRunner/ProcessHandle | SKIPPED (justified: enables full SiteLifecycleManager testing via fake implementations) |
| 16 | shrink Logging project | ✅ PARTIAL (dropped Starting/Duration events: 30 methods, 18 OperationTimer callbacks; dropped client UI logging: 35 methods across Home/Dialogs; kept LicenseService + CultureManager client logs) |
| 17 | native AspNetCoreReleaseType | SKIPPED (justified: Data project avoids external library dependency; enum mirrors library values for clean boundary) |
| 18 | delete EmptyResponse | ✅ DONE |
| 19 | shrink AspNetChannel | ✅ DONE |
| 20 | shrink WebSiteInstance inheritance | ✅ DONE |
| 21 | shrink WebSiteRuntimeState factories | ✅ DONE |
| 22 | shrink DsmParameterNameAttribute | ⬜ PENDING |
| 23 | shrink LocalizedText + ResourceManagerCache | ⬜ PENDING |

## Dependencies Removable

| Package | Version | Priority | Reason |
|---|---|---|---|
| `BenchmarkDotNet` | 0.15.8 | P0 | Sole consumer is Benchmarks project (deleted) |
| `Microsoft.Extensions.Logging` | 10.0.9 | P2 | Solely for `[LoggerMessage]`; removable if logging is collapsed |

## Execution Plan

### Week 1 — P0 Quick Wins (Items 17-18)

**Item 17: native AspNetCoreReleaseType (25 lines)**

- Replace `AspNetCoreReleaseType` with `Microsoft.Deployment.DotNet.Releases.ReleaseType` in `AspNetCoreReleaseInfo`
- Remove `ConvertReleaseType` cast in `DownloaderService`
- Update 3 test files
- **Risk:** Low — enum values match exactly (LTS=0, STS=1, Unknown=99)
- **Verification:** Build + existing tests pass

**Item 18: delete EmptyResponse (8 lines)**

- Replace `ApiResponseBase<EmptyResponse>` with `ApiResponseBase<object>` in `DsmApiClient.ExecuteSimpleAsync`
- Update 5 test references
- **Risk:** Low — empty class carries no behavioral contract
- **Verification:** Build + existing tests pass

### Week 2 — P1 Domain Shrinks (Items 19-22)

**Item 19: shrink AspNetChannel (47 lines)**

- Add `ToString()` to `AspNetCoreReleaseInfo` (format: `"8.0 (LTS)"` or `"9.0"`)
- Replace `AspNetChannel` usages in `DotnetVersionService`, `RuntimeManagementController`, `ChannelsResult`
- Update 10 test references
- **Risk:** Medium — 4 prod consumers, touches API return types
- **Verification:** Build + integration tests for channel endpoints

**Item 20: shrink WebSiteInstance inheritance (67 lines)**

- Merge `WebSiteInstanceDetails` into `WebSiteInstance` with `[JsonIgnore]` on `Process`
- Update `WebSiteHostingService.SiteEntry`, `UpdateInstanceRuntimeState`
- Update 6 test references
- **Risk:** Medium — JSON serialization boundary changes
- **Verification:** Build + serialization tests + API response validation

**Item 21: shrink WebSiteRuntimeState factories (18 lines)**

- Replace `WebSiteRuntimeState.Stopped` → `new(false, null, "Stopped")` at 4 call sites
- Replace `WebSiteRuntimeState.Running(info)` → `new(true, info, "Running")` at 1 call site
- Update 2 test references
- **Risk:** Low — mechanical replacement, primary constructor is public
- **Verification:** Build + existing tests pass

**Item 22: shrink DsmParameterNameAttribute (7 lines)**

- Add `protected virtual string JsonParameterName { get; }` to `ApiParametersBase`
- Remove attribute from parameter types, override property instead
- **Risk:** Low — 1 consumer, mechanical replacement
- **Verification:** Build + DSM API integration tests

### Week 3 — P2 Deferred (Item 23)

**Item 23: shrink LocalizedText + ResourceManagerCache (30 lines)**

- Change `ILocalizer` indexer to return `string` directly
- Remove `LocalizedText` class and implicit operator
- Inline `ResourceManagerCache.SharedResource` into `Localizer` constructor
- Update 10 consumer sites (mostly tests)
- **Risk:** Medium — 10 consumers, implicit operator removal may expose type mismatches
- **Verification:** Build + all globalization tests

## Not Counted (Defensible Design)

- **6 dual-implementation interfaces** (server + client proxy for Blazor WASM interop)
- **DSM API models** (68 thin DTOs in `Data/DsmApi/`, not over-engineered)
- **Custom `Localizer` / `ILocalizer`** (addresses real WASM culture caching bug in `IStringLocalizer`)
- **`DsmApiClient`** (342 lines, but handles session TTL, handshake, auth, form/JSON dispatch — domain-specific, not reinvented)
- **`IApiResponse` / `IApiParameters`** (generic constraints on `DsmApiClient`, not YAGNI — used as type bounds in 5+ method signatures)

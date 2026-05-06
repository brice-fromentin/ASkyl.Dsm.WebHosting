# Test Project Review Findings

**Date:** May 4, 2026
**Scope:** `src/Askyl.Dsm.WebHosting.Tests/` (16 files, 187 tests)
**Status:** Pending fixes

---

## Critical (2)

### 1. Production bug — `ArchiveExtractorService` calls `GetDirectory("")` which throws ✅ FIXED

**File:** `ArchiveExtractorService.cs:36` / `ArchiveExtractorServiceTests.cs:30`

The service called `fileManager.GetDirectory(String.Empty)` which throws `ArgumentException` via `SanitizePathSegment`. Tests passed because the mock intercepted the call, but **production would crash**.

**Fix applied:**

- Added `allowEmpty` parameter to `SanitizePathSegment` (no default, all callers explicit) — `GetDirectory` passes `true`, others `false`
- This preserves the semantic that `GetDirectory("")` means "give me the root"
- Updated tests: `GetDirectory_EmptyName_ThrowsArgumentException` → `GetDirectory_EmptyName_ReturnsRootDirectory`, and null test now expects `ArgumentNullException`

---

### 2. Zip-slip test depends on Python being installed ✅ FIXED

**File:** `ArchiveExtractorServiceTests.cs:163-194`

`CreateArchiveWithZipSlip()` spawned `python3`. If not available (CI, non-Linux), the tests silently failed.

**Fix applied:** Replaced Python subprocess with pure C# using `TarWriter.WriteEntry()` to create the malicious `../../escaped.txt` archive entry directly.

---

## Suggestion (3)

### 3. HttpClient leak in ExtensionMethodsTests ✅ FIXED

**File:** `ExtensionMethodsTests.cs` (all async test methods)

9+ test methods each created an `HttpClient` without disposing. While .NET 10 has improved connection pooling, each `HttpClient` still holds a `ConnectionLease` to the socket pool.

**Fix applied:**

- Class implements `IDisposable` with a single shared `MockHttpMessageHandler`
- Each test calls `SetResponse()` to configure, then `CreateClient()` to wrap
- Handler disposed once at test class teardown

---

### 4. ResultTypesTests: 350 lines of duplicated tests ✅ FIXED

**File:** `ResultTypesTests.cs` (entire file, ~350 lines)

Same 3 test patterns (CreateSuccess, CreateFailure-default, CreateFailure-custom) repeated for 12+ Result types. Nearly identical.

**Fix applied:**

- 7 `ApiResultItems<T>` types collapsed into 2 parameterized `[Theory]` tests using `[MemberData]` with delegates and `dynamic` property access
- 5 unique types (ApiResult, ApiResultBool, AuthenticationResult, InstallationResult, WebSiteInstanceResult) kept as individual tests due to type-specific assertions
- Reduced from ~350 to ~170 lines; test count reduced from 183 to 176

---

### 5. Magic numbers in validation tests ✅ FIXED

**File:** `WebSiteConfigurationTests.cs` (lines 112, 121, 130, 139 for port; lines 234, 243, 252, 261 for timeout)

Port validation tests used hardcoded values `1023`, `65536` instead of constants. Timeout tests used `9`, `121` instead of constants.

**Fix applied:**

- Added private class constants `PortMin`, `PortMax`, `TimeoutMin`, `TimeoutMax`, `NameMaxLength`
- Replaced all magic numbers with expressions like `PortMin - 1`, `TimeoutMax + 1`, `NameMaxLength + 1`

---

## Nice to Have (5)

### 6. Dispose cleanup can leave orphaned directories ✅ FIXED

**File:** `ArchiveExtractorServiceTests.cs:23-28`

`Dispose()` deletes `_tempBase` then `_tempExtract` sequentially. If the first `Directory.Delete` throws, the second is never cleaned up.

**Fix applied:** Each `Directory.Delete` is wrapped in its own `try/catch` so both cleanup attempts run independently.

---

### 7. Hardcoded directory names in FileManager test ✅ FIXED

**File:** `FileManagerServiceTests.cs:137-140`

`Initialize_CreatesDefaultDirectories` hardcoded `"downloads"` and `"temp"` instead of using `InfrastructureConstants.Downloads` and `InfrastructureConstants.TempDirectory`.

**Fix applied:** Replaced with `InfrastructureConstants.Downloads` and `InfrastructureConstants.TempDirectory`.

---

### 8. FluentAssertions dependency is unused ✅ FIXED

**File:** `Askyl.Dsm.WebHosting.Tests.csproj` (line 15)

The project referenced `FluentAssertions` 8.9.0 but none of the 13 test files imported or used it. All assertions used the xUnit `Assert` API.

**Fix applied:** Removed the FluentAssertions package reference.

---

### 9. Tests of internal methods via InternalsVisibleTo are tightly coupled ✅ FIXED

**File:** `VersionsDetectorServiceTests.cs:168-204`

`TryAddFrameworkFromRegex_*` tests directly invoked `internal` methods (`DetectCurrentSection`, `TryAddFrameworkFromRegex`, `SdkVersionRegex`). Tightly coupled to implementation details.

**Fix applied:**

- Made `ParseDotnetInfo` public (it's the natural entry point for parsing logic)
- Replaced all internal method tests with crafted `ParseDotnetInfo` input strings that exercise the same section detection, regex matching, and deduplication logic
- Removed `InternalsVisibleTo` from the Tools project (AssemblyInfo.cs deleted, project property removed)
- Tests no longer depend on internal implementation details

---

### 10. Non-atomic maxConcurrent write in concurrency test ✅ FIXED

**File:** `SemaphoreLockTests.cs:85`

`maxConcurrent = Math.Max(maxConcurrent, concurrentCount)` was a non-atomic read-modify-write. Works correctly because the semaphore serializes execution, but the pattern was misleading.

**Fix applied:** Replaced with `Interlocked.CompareExchange` loop for atomic max update.

---

## Summary

| Severity | Count | Status |
|----------|-------|--------|
| Critical | 2 | ✅ Fixed |
| Suggestion | 3 | ✅ Fixed |
| Nice to have | 5 | ✅ Fixed |
| **Total** | **10** | ✅ All Fixed |

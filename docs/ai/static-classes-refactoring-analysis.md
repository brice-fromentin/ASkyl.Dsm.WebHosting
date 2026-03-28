# Static Classes Refactoring Analysis

**Branch:** `feature/refactor-static-classes-for-testing`  
**Date:** March 28, 2026 (Updated: March 28, 2026)  
**Purpose:** Re-architecture static classes to improve code maintainability and align with existing DI patterns

---

## Executive Summary

This document identifies **31 static classes** and **46 partial classes** in the codebase that impact code architecture. The analysis reveals **5 critical infrastructure classes** in `Askyl.Dsm.WebHosting.Tools` that require refactoring to align with the solution's established dependency injection patterns documented in [Technical Architecture v0.5.2](technical-architecture.md).

> **Note:** This refactoring focuses on architectural improvements only. Unit test implementation is out of scope for this initiative.

---

## ✅ Completed Refactoring: PlatformInfo & Downloader

### Status: COMPLETED (March 28, 2026)

The `PlatformInfo` static class and `Downloader` have been successfully refactored following the architectural patterns documented in this analysis.

#### Files Created
1. **`Data/Contracts/IPlatformInfo.cs`** - Interface for platform information abstraction
2. **`Tools/Infrastructure/PlatformInfoService.cs`** - Injectable service implementation
3. **`Constants/Runtime/RuntimeConstants.cs`** - Architecture and OS string constants

#### Files Modified
1. **`Tools/Runtime/Downloader.cs`** - Converted from static class to DI-based service
2. **`Ui/Services/FrameworkManagementService.cs`** - Injects `IPlatformInfo` and `Downloader`
3. **`Ui/Services/DotnetVersionService.cs`** - Injects `Downloader`
4. **`Ui/Program.cs`** - Registers services as singletons
5. **`DotnetInstaller/Program.cs`** - Manually instantiates with console logger

#### Files Deleted
1. **`Tools/Infrastructure/PlatformInfo.cs`** - Old static class removed

---

### Implementation Details: PlatformInfoService

#### Key Design Decisions

**1. Required ILogger Dependency**
```csharp
public PlatformInfoService(ILogger<PlatformInfoService> logger)
{
    _logger = logger;
    InitializePlatformInfo();
}
```
- **Rationale:** Clear dependency contract, consistent with other services in the codebase
- **Benefit:** Structured logging for diagnostic information at startup

**2. Required Configuration File (Fail-Fast)**
```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile(ApplicationConstants.SettingsFileName, optional: false, reloadOnChange: false)
    .Build();

var channelVersion = configuration[ApplicationConstants.ChannelVersionKey];
if (String.IsNullOrWhiteSpace(channelVersion))
{
    throw new InvalidOperationException(
        $"Required setting '{ApplicationConstants.ChannelVersionKey}' is missing or empty...");
}
```
- **Rationale:** Settings file is required for application to function properly
- **Benefit:** Catches misconfiguration early with clear error messages

**3. Extracted Magic Strings to Constants**
```csharp
// Before: "x64", "arm", "linux", etc. as magic strings
CurrentArchitecture = osArchitecture switch
{
    Architecture.X64 => "x64",  // ❌ Magic string
    // ...
};

// After: Constants from RuntimeConstants
CurrentArchitecture = MapArchitectureToOsString(osArchitecture);

private static string MapArchitectureToOsString(Architecture architecture)
{
    return architecture switch
    {
        Architecture.X64 => RuntimeConstants.ArchitectureX64,  // ✅ Constant
        // ...
    };
}
```

**4. Refactored DEBUG/RELEASE Logic Duplication**
```csharp
// Before: Duplicated switch expressions with #if/#else
#if DEBUG
    CurrentOS = platform switch { /* 3 cases */ };
#else
    CurrentOS = platform switch { /* 1 case */ };
#endif

// After: Single method with parameter
CurrentOS = MapPlatformToOsString(platform, allowAllPlatforms: IsDebugMode());
```

**5. Simplified Configuration Loading**
```csharp
// Before: Redundant File.Exists check + duplicate String.Empty assignment
if (File.Exists(configFilePath)) { /* ... */ }
else { ChannelVersion = String.Empty; }

// After: Clean, single path with optional file handling
var configuration = new ConfigurationBuilder()...Build();
ChannelVersion = configuration[key] ?? String.Empty;
```

---

### Implementation Details: Downloader Service

#### Conversion from Static to DI-Based Service

**Before (Static Class):**
```csharp
public static class Downloader
{
    public static async Task<string> DownloadToAsync(bool skipDownloadIfExists = false)
    {
        var product = await GetProductAsync(PlatformInfo.ChannelVersion, true);
        // ... uses PlatformInfo and FileManager statically
    }
}
```

**After (Injectable Service):**
```csharp
public sealed class Downloader(IPlatformInfo platformInfo)
{
    public async Task<string> DownloadToAsync(bool skipDownloadIfExists = false)
    {
        var product = await GetProductAsync(platformInfo.ChannelVersion, true);
        // ... uses injected dependencies
    }
}
```

#### Registration in DI Container
```csharp
// Program.cs
builder.Services.AddSingleton<IPlatformInfo, PlatformInfoService>();
builder.Services.AddSingleton<Downloader>();  // Depends on IPlatformInfo
```

---

### Standalone Application Support: DotnetInstaller

For the standalone console application without a full DI container:

```csharp
// Create a simple console logger for the standalone application
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<PlatformInfoService>();

var platformInfo = new PlatformInfoService(logger);
var downloader = new Downloader(platformInfo);

var fileName = await downloader.DownloadToAsync(true);
```

**Package Added:** `Microsoft.Extensions.Logging.Console` v10.0.5

---

### Verification

✅ **Build Status:** Successful with no errors or warnings  
✅ **Code Compliance:** All formatting rules verified (using directives, blank lines, String/string pattern)  
✅ **Architecture Alignment:** Follows existing DI patterns from Technical Architecture v0.5.2  

---

## 🔴 Critical Priority - Remaining Refactoring Required

These static classes still require refactoring:

### 1. FileManager ⚠️ STILL REQUIRED
**File:** `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/FileManager.cs`  
**Impact:** CRITICAL

**Current Issues:**
- Global mutable state: `_dotnetRoot`, `_existingFolders` (ConcurrentDictionary)
- Direct file system operations (`Directory.CreateDirectory`, `Directory.Delete`)
- Cannot isolate tests or mock file system interactions
- State leaks between test methods causing flaky tests
- Race conditions possible in parallel test execution

**Key Methods:**
```csharp
public static string GetDirectory(string name)
public static void DeleteDirectory(string directory)
public static string GetFullName(string name)
```

**Refactoring Strategy:**
1. Create `IFileManager` interface with file operations
2. Implement `FileManagerService` with real file system logic
3. Remove static state; use instance properties with DI scope
4. Ensure backward compatibility during transition period

---

### 2. VersionsDetector ⚠️ STILL REQUIRED
**File:** `src/Askyl.Dsm.WebHosting.Tools/Runtime/VersionsDetector.cs`  
**Impact:** CRITICAL

**Current Issues:**
- Static cache `_cachedFrameworks` shared across all callers
- Spawns external processes (`Process.Start("dotnet", "--info")`)
- Cannot mock process execution or test different output scenarios
- Cache state leaks between tests
- Tests must execute real processes or skip validation

**Key Methods:**
```csharp
public static async Task<IEnumerable<InstalledFramework>> GetInstalledFrameworksAsync()
private static IEnumerable<InstalledFramework> ParseDotnetInfo(string output)
```

**Refactoring Strategy:**
1. Create `IProcessExecutor` interface for process spawning
2. Create `IVersionsDetector` interface with detection methods
3. Implement `VersionsDetectorService` with injectable dependencies
4. Add caching as optional service layer (not static field)
5. Ensure backward compatibility during transition period

---

### 3. ArchiveExtractor ⚠️ STILL REQUIRED
**File:** `src/Askyl.Dsm.WebHosting.Tools/Infrastructure/ArchiveExtractor.cs`  
**Impact:** HIGH

**Current Issues:**
- Calls static `FileManager.GetDirectory()` (static dependency)
- Direct file I/O operations (`File.OpenRead`, ZipArchive)
- Cannot mock file system or test error scenarios easily
- Tests require actual archive files and disk space

**Key Methods:**
```csharp
public static async Task ExtractAsync(string archivePath, string targetDirectory)
private static void ValidateArchive(string archivePath)
```

**Refactoring Strategy:**
1. Inject `IFileManager` for directory operations
2. Use dependency injection for file access abstractions
3. Implement `ArchiveExtractorService` with mockable dependencies
4. Ensure backward compatibility during transition period

---

## 📊 Summary of Completed Work

### Phase 1: PlatformInfo & Downloader ✅ COMPLETED

| Component | Status | Files Changed | Build Status |
|-----------|--------|---------------|--------------|
| **PlatformInfo** | ✅ Refactored to `PlatformInfoService` | 3 created, 1 deleted | ✅ Success |
| **Downloader** | ✅ Converted from static class | 1 modified | ✅ Success |
| **Consumers Updated** | ✅ All consumers migrated | 4 files modified | ✅ Success |

### Remaining Critical Components ⚠️ PENDING

| Component | Priority | Dependencies | Estimated Effort |
|-----------|----------|--------------|------------------|
| **FileManager** | CRITICAL | None (core dependency) | Medium-High |
| **VersionsDetector** | CRITICAL | `IProcessExecutor` | Medium |
| **ArchiveExtractor** | HIGH | Depends on `FileManager` | Low-Medium |

---

## 🎯 Next Steps

1. **Refactor FileManager** - Most critical due to global mutable state and file system operations
2. **Refactor VersionsDetector** - Extract process execution abstraction
3. **Refactor ArchiveExtractor** - Will depend on refactored FileManager
4. **Update remaining consumers** - Ensure all static references are migrated

---

## 🔗 Related Documentation

- [Technical Architecture v0.5.2](technical-architecture.md)
- Branch: `feature/refactor-static-classes-for-testing`

---

**Document Updated:** March 28, 2026  
**Status:** PlatformInfo & Downloader refactoring completed successfully

---

## 🟡 Medium Priority - Extension Method Classes

These static classes contain extension methods that are generally testable but have some limitations:

### HttpClientExtensions
**File:** `src/Askyl.Dsm.WebHosting.Tools/Extensions/HttpClientExtensions.cs`  
**Impact:** LOW-MEDIUM

**Issue:** Uses `JsonOptionsCache.Options` (static)  
**Solution:** Inject `JsonSerializerOptions` or create interface for JSON serialization

---

## 🟢 Low Priority - Constants Classes

These **21 static classes** are appropriate uses of static for constants and do NOT require refactoring:

### Application Constants
- `ApplicationConstants`
- `LicenseConstants`
- `LogConstants`

### DSM API Constants
- `ApiVersions`, `ApiNames`, `ApiMethods`
- `ReverseProxyConstants`
- `FileStationDefaults`, `PaginationDefaults`
- `SystemDefaults`

### Web API Routes
- `AuthenticationRoutes`
- `FileManagementRoutes`
- `FrameworkManagementRoutes`
- `LicenseRoutes`
- `LogDownloadRoutes`
- `RuntimeManagementRoutes`
- `WebsiteHostingRoutes`

### UI Constants
- `DialogConstants`
- `FileSizeConstants`

### Other Constants
- `DotNetFrameworkTypes`
- `NetworkConstants`
- `JsonOptionsCache` (can remain static; JSON options are configuration, not behavior)

---

## 🟡 Medium Priority - Static Methods in Services

These private static methods in non-static classes reduce testability:

### WebSiteHostingService
**File:** `src/Askyl.Dsm.WebHosting.Ui/Services/WebSiteHostingService.cs`

| Method | Issue | Solution |
|--------|-------|----------|
| `ConfigurationRequiresRestart()` | Private static - logic not directly testable | Make `protected internal` with `[InternalsVisibleTo]` or extract to separate service |
| `WaitForProcessExitAsync()` | Spawns processes, cannot mock | Extract to `IProcessManager` interface |
| `CreateProcessStartInfo()` | Creates process configuration, hard to test | Extract to factory class with interface |

### FileSystemService
**File:** `src/Askyl.Dsm.WebHosting.Ui/Services/FileSystemService.cs`

| Method | Issue | Solution |
|--------|-------|----------|
| `CreateFsEntry(FileStationFile)` | Private static factory - indirect testing only | Make protected internal or extract to factory |
| `CreateFsEntry(FileStationShare)` | Same as above | Same as above |

### ReverseProxyManagerService
**File:** `src/Askyl.Dsm.WebHosting.Ui/Services/ReverseProxyManagerService.cs`

| Method | Issue | Solution |
|--------|-------|----------|
| `IsNotFoundError(int?)` | Private static validation - buried logic | Make protected internal for direct testing |
| `IsNotFoundError(string)` | Same as above | Same as above |
| `GetDescription()` | Private static string builder - trivial but isolated | Can remain private; low test value |

### SemaphoreLock
**File:** `src/Askyl.Dsm.WebHosting.Tools/Threading/SemaphoreLock.cs`

| Method | Issue | Solution |
|--------|-------|----------|
| `Acquire()`, `AcquireAsync()` | Static factory on sealed class - cannot substitute mocks | Create `ISemaphoreLockFactory` interface for test scenarios |

---

## 📋 Partial Classes Analysis

### Static Partial Classes (3 total)
- **VersionsDetector** - Already covered in Critical Priority
- **UriExtensions** - Extension methods, low impact
- **HelloWorldExtensions** - Source-generated logging, no action needed

### Non-Static Partial Classes (46 total)
These are data models with generated clone methods. Generally testable as-is:
- `ApiParametersNone`
- `WebSiteConfiguration`, `WebSiteInstance`
- FileStation models (19 files)
- ReverseProxy models (6 files)
- Core models (5 files)

**Recommendation:** No refactoring needed for partial classes unless they contain static members.

---

## 🎯 Refactoring Plan - Phase 1 (Critical)

### Week 1: Infrastructure Layer

#### Day 1-2: PlatformInfo Refactoring
1. Create `IPlatformInfo` interface
2. Implement `PlatformInfoService` with DI support
3. Update all consumers (~15 files estimated)
4. Verify integration and backward compatibility

#### Day 3-4: FileManager Refactoring
1. Design `IFileManager` interface or evaluate existing abstractions
2. Implement `FileManagerService` with file system abstraction
3. Remove static state (`_dotnetRoot`, `_existingFolders`)
4. Update all consumers (~20 files estimated)
5. Verify integration and backward compatibility

#### Day 5: VersionsDetector Refactoring
1. Create `IProcessExecutor` interface
2. Create `IVersionsDetector` interface
3. Implement `VersionsDetectorService` with injectable dependencies
4. Add caching as optional service layer
5. Update consumers (~5 files estimated)
6. Verify integration and backward compatibility

### Week 2: Runtime Layer

#### Day 1-2: Downloader Refactoring
1. Create `IDotNetReleaseService` interface (wrap Microsoft.Deployment.DotNet.Releases)
2. Implement `DownloaderService` with full DI
3. Inject `IPlatformInfo`, `IFileManager`, `IDotNetReleaseService`
4. Update consumers (~3 files estimated)
5. Verify integration and backward compatibility

#### Day 3: ArchiveExtractor Refactoring
1. Inject `IFileManager` for directory operations
2. Use dependency injection for file access abstractions
3. Implement `ArchiveExtractorService`
4. Update consumers (~2 files estimated)
5. Verify integration and backward compatibility

#### Day 4-5: Integration & Verification
1. Verify all critical paths work end-to-end
2. Run existing test suite (if any) to ensure no regressions
3. Add integration tests for refactored components
4. Document new architectural patterns

---

## 🎯 Refactoring Plan - Phase 2 (Medium Priority)

### Week 3: Service Layer Cleanup

#### Day 1-2: Static Methods Extraction
1. Extract private static methods from services to testable classes
2. Apply `[InternalsVisibleTo]` for internal access where appropriate
3. Create factory interfaces where needed (`ISemaphoreLockFactory`)
4. Update service implementations

#### Day 3-4: Extension Method Review
1. Address `HttpClientExtensions` JSON serialization dependency
2. Consider injecting `JsonSerializerOptions` or creating abstraction
3. Document extension method architectural patterns

#### Day 5: Final Verification & Documentation
1. Comprehensive integration verification
2. Update developer documentation with new patterns
3. Create "Architecture Guidelines" guide for future development

---

## 📊 Impact Assessment

### Files to Modify (Estimated)
- **Phase 1:** ~45 files (infrastructure + runtime consumers)
- **Phase 2:** ~10 files (service layer cleanup)
- **Total:** ~55 files across the solution

### New Interfaces/Services to Create
1. `IPlatformInfo` + `PlatformInfoService`
2. `IFileManager` + `FileManagerService` (or use System.IO.Abstractions)
3. `IProcessExecutor` + `ProcessExecutorService`
4. `IVersionsDetector` + `VersionsDetectorService`
5. `IDotNetReleaseService` + `DotNetReleaseService`
6. `IArchiveExtractor` + `ArchiveExtractorService`
7. `ISemaphoreLockFactory` (optional)

### Architectural Benefits
✅ Decouple platform detection for better maintainability  
✅ Isolate file system operations with abstraction layer  
✅ Abstract process execution for cleaner architecture  
✅ Eliminate static dependencies for better separation of concerns  
✅ Remove global state to prevent race conditions  
✅ Improve code modularity and reusability  

---

## 🛠️ Recommended Libraries

### For File System Abstraction
**TestableIO.System.IO.Abstractions** (NuGet package)
- Provides `IFileSystem`, `IDirectory`, `IFile` interfaces
- Industry standard for file system abstraction in .NET
- Enables clean separation between business logic and file I/O

Alternative: Create custom abstractions if specific needs exist.

### For Process Execution Abstraction
Create custom `IProcessExecutor`:
```csharp
public interface IProcessExecutor
{
    Task<string> ExecuteAsync(string fileName, string arguments, CancellationToken cancellationToken = default);
}
```

This abstraction enables cleaner architecture and separation of concerns for process management.
```

---

## ⚠️ Risks & Mitigations

### Risk 1: Breaking Changes
**Impact:** High - All consumers must be updated  
**Mitigation:** 
- Use feature branch (`feature/refactor-static-classes-for-testing`)
- Comprehensive integration testing before merge
- Gradual rollout with feature flags if needed

### Risk 2: Performance Overhead
**Impact:** Low-Medium - DI and abstraction layers add minimal overhead  
**Mitigation:** 
- Benchmark critical paths before/after
- Use compiled lambdas where appropriate
- Cache expensive operations at service level (not static)

### Risk 3: Integration Issues
**Impact:** Medium - New abstractions may introduce integration complexity  
**Mitigation:** 
- Comprehensive integration verification after each phase
- Maintain backward compatibility during transition period
- Add integration tests for end-to-end scenarios

---

## 📝 Next Steps

1. **Review this analysis** with team/stakeholders
2. **Prioritize Phase 1** (Critical infrastructure)
3. **Create detailed task breakdown** for each day in the plan
4. **Begin refactoring** with `PlatformInfo` as proof of concept
5. **Verify integration** after each component is refactored

---

## 🔗 Related Documentation

- [Synology DSM API Guide](https://global.download.synology.com/download/Document/DeveloperGuide/Synology_File_Station_API_Guide.pdf)
- [System.IO.Abstractions Documentation](https://github.com/TestableIO/System.IO.Abstractions)
- [.NET Dependency Injection Best Practices](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

---

**Generated by:** Qwen Code AI Assistant  
**Session Date:** March 28, 2026  
**Branch:** `feature/refactor-static-classes-for-testing`

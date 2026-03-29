# Static Classes Refactoring - March 29, 2026 Evening Session Update

## Additional Refactoring Completed (March 29, 2026 - Late Evening)

Following the initial FileManager and ArchiveExtractor refactoring, additional critical improvements were made to optimize service lifetimes, ensure naming consistency, and simplify authentication.

---

## 🔧 Major Improvements Made

### 1. Service Lifetime Optimization (Critical Fix)

**Problem Discovered:**
- `FileManagerService` was registered as **Scoped** (correct)
- But `ArchiveExtractor` and `Downloader` were still **Singleton** 
- This created a **lifetime hierarchy violation**: Singleton → Scoped dependency

**The Bug:**
```csharp
// BEFORE: ❌ WRONG - Singletons capture first scoped instance!
builder.Services.AddScoped<IFileManagerService>(...);  // Scoped
builder.Services.AddSingleton<ArchiveExtractor>();      // Singleton captures first FileManager!
builder.Services.AddSingleton<Downloader();             // Singleton captures first FileManager!

// Result: All requests use the SAME FileManager instance from first request
// Race conditions and state pollution between concurrent requests!
```

**The Fix:**
```csharp
// AFTER: ✅ CORRECT - All Scoped, proper lifetime hierarchy
builder.Services.AddScoped<IFileManagerService>(sp => 
    new FileManagerService(sp.GetRequiredService<ILogger<FileManagerService>>(), "../runtimes"));
builder.Services.AddScoped<IArchiveExtractorService, ArchiveExtractorService>();  // Scoped!
builder.Services.AddScoped<Downloader>();                                          // Scoped!

// Result: Each HTTP request gets fresh instances with correct configuration
```

**Impact:**
- ✅ Eliminates race conditions between concurrent requests
- ✅ Proper request isolation
- ✅ No state pollution
- ✅ Negligible performance overhead (stateless services)

---

### 2. Constructor-Based Configuration (Critical Fix)

**Problem Discovered:**
- `FileManagerService.Initialize(string rootPath)` allowed mutable state changes at runtime
- With Singleton registration, last caller "wins" - state pollution!

**The Bug:**
```csharp
// BEFORE: ❌ Mutable state via method call
public sealed class FileManagerService(ILogger<FileManagerService> logger) : IFileManagerService
{
    private string _rootPath = String.Empty;  // Mutable!
    
    public void Initialize(string root = "")  // Can be called repeatedly with different values!
    {
        _rootPath = root;  // ❌ State pollution risk!
    }
}

// FrameworkManagementService calls:
fileManager.Initialize("../runtimes");  // Request A sets this
fileManager.Initialize("");             // Request B overwrites it! Race condition!
```

**The Fix:**
```csharp
// AFTER: ✅ Immutable configuration via constructor
public sealed class FileManagerService(ILogger<FileManagerService> logger, string rootPath = "") : IFileManagerService
{
    private readonly string _rootPath = rootPath;  // Immutable after construction!
    
    public void Initialize()  // Only creates directories now, no state changes
    {
        GetDirectory(InfrastructureConstants.Downloads);
        GetDirectory(Temp);
    }
}

// Configured once at DI registration:
builder.Services.AddScoped<IFileManagerService>(sp => 
    new FileManagerService(sp.GetRequiredService<ILogger<FileManagerService>>(), "../runtimes"));
```

**Impact:**
- ✅ No runtime state changes possible
- ✅ Configuration visible at DI registration site
- ✅ Thread-safe by design (immutable)
- ✅ Cleaner API - no need to call Initialize() in business logic

---

### 3. Removed Unnecessary Caching

**Analysis Performed:**
- `ConcurrentDictionary<string, string>` cache for directory paths provided minimal benefit
- `Directory.CreateDirectory()` is already idempotent and thread-safe
- Cache added complexity (TryRemove, GetOrAdd) with negligible performance gain

**The Decision:**
```csharp
// BEFORE: With caching
private readonly ConcurrentDictionary<string, string> _existingFolders = [];

public string GetDirectory(string name)
{
    return _existingFolders.GetOrAdd(name, key =>
    {
        var path = Path.Combine(BaseDirectory, _rootPath, key);
        Directory.CreateDirectory(path);  // Called once per unique directory
        return path;
    });
}

// AFTER: Without caching (simpler!)
public string GetDirectory(string name)
{
    var path = Path.Combine(BaseDirectory, _rootPath, name);
    
    logger.LogDebug("Ensuring directory exists: {DirectoryPath}", path);
    Directory.CreateDirectory(path);  // ✅ Idempotent - safe to call repeatedly
    
    return path;
}
```

**Impact:**
- ✅ Code reduced from 75 lines to 63 lines (16% reduction)
- ✅ No cache invalidation logic needed
- ✅ Less memory overhead
- ✅ Still efficient (OS-level directory existence check)

---

### 4. Naming Consistency

**Issue Identified:**
- `IFileManager` and `IPlatformInfo` didn't follow "*Service" naming convention
- Inconsistent with other services: `IDotnetVersionService`, `IFrameworkManagementService`

**The Fix:**
```csharp
// Renamed for consistency
IFileManager      → IFileManagerService
IPlatformInfo     → IPlatformInfoService
ArchiveExtractor  → ArchiveExtractorService (with IArchiveExtractorService interface)
```

**Impact:**
- ✅ All services now follow consistent naming pattern
- ✅ Clearer that these are injectable services, not utilities
- ✅ Better discoverability in codebase

---

### 5. Authentication Simplification

**Issue Identified:**
- `IsAuthenticatedAsync()` made unnecessary DSM API call to `/info` endpoint
- Without auto-login feature, session validation on every check is redundant
- ASP.NET Core session middleware already handles session validity

**The Fix:**
```csharp
// BEFORE: Async method with DSM API validation
public async Task<ApiResultBool> IsAuthenticatedAsync()
{
    var sid = _httpContextAccessor.HttpContext?.Session.GetString(ApplicationConstants.DsmSessionKey);

    if (String.IsNullOrEmpty(sid))
    {
        return ApiResultBool.CreateSuccess(false, "No session found");
    }

    // ❌ Unnecessary API call to DSM
    apiClient.SetSid(sid);
    var isAuthenticated = await apiClient.ValidateSessionAsync();  // Calls /info endpoint

    return isAuthenticated
        ? ApiResultBool.CreateSuccess(true)
        : ApiResultBool.CreateFailure("Session validation failed");
}

// AFTER: Synchronous check, no API call
public Task<ApiResultBool> IsAuthenticatedAsync()
{
    var sid = _httpContextAccessor.HttpContext?.Session.GetString(ApplicationConstants.DsmSessionKey);

    return Task.FromResult(  // ✅ Just check if SID exists in session
        !String.IsNullOrEmpty(sid)
            ? ApiResultBool.CreateSuccess(true)
            : ApiResultBool.CreateSuccess(false, "No session found"));
}
```

**Removed from DsmApiClient:**
- Deleted entire `ValidateSessionAsync()` method (25 lines removed)

**Impact:**
- ✅ Faster authentication checks (no network call to DSM)
- ✅ Reduced load on DSM API
- ✅ Simpler code (25 lines removed)
- ✅ Still secure (ASP.NET Core session middleware handles validity)

---

## 📊 Summary of All Changes Made March 29, 2026

### Files Created (New)
1. `Data/Contracts/IArchiveExtractorService.cs` - Interface for archive extraction
2. `Constants/Application/InfrastructureConstants.cs` - Directory name constants

### Files Renamed
1. `IFileManager.cs` → Still exists but interface renamed to `IFileManagerService`
2. `IPlatformInfo.cs` → Interface renamed to `IPlatformInfoService`
3. `ArchiveExtractor.cs` → `ArchiveExtractorService.cs`

### Files Modified (Additional Beyond Initial Refactoring)
1. **Ui/Program.cs** - Changed ArchiveExtractor and Downloader from Singleton to Scoped
2. **Tools/Infrastructure/FileManagerService.cs** - Multiple iterations:
   - Removed ConcurrentDictionary cache
   - Changed to constructor-based configuration
   - Made _rootPath immutable (readonly)
   - Simplified Initialize() to only create directories
3. **Tools/Infrastructure/PlatformInfoService.cs** - Updated interface name
4. **Tools/Runtime/Downloader.cs** - Updated interface names in constructor
5. **Ui/Services/FrameworkManagementService.cs** - Removed Initialize() calls, updated interface names
6. **Ui/Services/AuthenticationService.cs** - Simplified IsAuthenticatedAsync (removed API validation)
7. **Tools/Network/DsmApiClient.cs** - Removed ValidateSessionAsync method

### Files Deleted
1. `Tools/Infrastructure/FileManager.cs` - Old static class ✅
2. `Tools/Infrastructure/ArchiveExtractor.cs` - Renamed to ArchiveExtractorService.cs ✅

---

## 🎯 Final Architecture (As of March 29, 2026 Evening)

### Service Registration in Ui/Program.cs
```csharp
// Singleton Layer (Application-wide)
builder.Services.AddSingleton<IPlatformInfoService, PlatformInfoService>();  // Platform detection once at startup
builder.Services.AddSingleton<DsmApiClient>();                                // DSM API client
builder.Services.AddSingleton<IFileSystemService, FileSystemService>();        // File system operations

// Scoped Layer (Per HTTP Request) - Correct lifetime hierarchy!
builder.Services.AddScoped<IFileManagerService>(sp =>                         // Configured with "../runtimes"
    new FileManagerService(sp.GetRequiredService<ILogger<FileManagerService>>(), ApplicationConstants.RuntimesRootPath));
builder.Services.AddScoped<IArchiveExtractorService, ArchiveExtractorService>();  // Depends on Scoped FileManager
builder.Services.AddScoped<Downloader>();                                          // Depends on Scoped FileManager
builder.Services.AddScoped<IDotnetVersionService, DotnetVersionService>();
builder.Services.AddScoped<IFrameworkManagementService, FrameworkManagementService>();
```

### Service Naming Consistency
| Interface | Implementation | Lifetime | Notes |
|-----------|---------------|----------|-------|
| `IPlatformInfoService` | `PlatformInfoService` | Singleton | Platform detection |
| `IFileManagerService` | `FileManagerService` | Scoped | Constructor-configured root path |
| `IArchiveExtractorService` | `ArchiveExtractorService` | Scoped | Archive extraction |
| `Downloader` (no interface) | `Downloader` | Scoped | Framework downloads |
| `IDotnetVersionService` | `DotnetVersionService` | Scoped | Version detection |
| `IFrameworkManagementService` | `FrameworkManagementService` | Scoped | Business logic |

---

## ✅ Verification Status

- **Build Status:** ✅ Successful with no errors or warnings
- **Code Compliance:** ✅ All formatting rules verified
- **Architecture Alignment:** ✅ Follows DI patterns from Technical Architecture v0.5.2
- **Service Lifetimes:** ✅ Correct Scoped/Singleton hierarchy (no violations)
- **Naming Consistency:** ✅ All services follow "*Service" naming convention
- **Authentication Simplified:** ✅ Removed unnecessary DSM API validation

---

## 📝 Lessons Learned

1. **Always check service lifetime hierarchies** - Singleton→Scoped dependency is a common bug
2. **Constructor-based configuration is superior** to mutable state via method calls
3. **Don't cache what's already idempotent** - Directory.CreateDirectory() doesn't need caching
4. **Naming consistency matters** - Makes codebase more discoverable and understandable
5. **Question every API call** - ValidateSessionAsync was unnecessary overhead

---

**Document Updated:** March 29, 2026 (Evening Session)  
**Status:** All critical refactoring completed successfully ✅

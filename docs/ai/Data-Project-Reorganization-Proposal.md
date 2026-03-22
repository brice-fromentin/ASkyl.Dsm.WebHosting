# Data Project Reorganization Proposal

**Date:** March 22, 2026  
**Branch:** `cleanup/solution-organization`  
**Status:** Proposal for Review

---

## Executive Summary

The Data project currently has organizational issues similar to what we found in the Constants project. This proposal outlines a comprehensive reorganization to improve clarity, maintainability, and separation of concerns.

**Current State:** 170+ files with mixed responsibilities  
**Proposed Impact:** ~150 files reorganized into clear domain boundaries

---

## Current Problems Identified

### 1. **API Folder Mixes External DSM APIs with Internal Concepts**

```
Current Structure:
Data/API/
├── Definitions/          ← DSM API request models (external)
│   ├── Core/
│   ├── FileStation/
│   └── ReverseProxy/
├── Parameters/           ← DSM API call parameters (external)
│   ├── AuthenticationAPI/
│   ├── FileStationAPI/
│   └── ReverseProxyAPI/
├── Requests/             ← Empty folder
└── Responses/            ← DSM API response models (external)
```

**Problem:** The `API` folder name is ambiguous - it could mean:
- External Synology DSM APIs (what it currently contains)
- Internal application WebAPI contracts
- Generic API-related code

### 2. **Inconsistent Naming Conventions**

| Current Pattern | Issue |
|----------------|-------|
| `FileStationCompressParameters.cs` | Mixes API name + operation + type |
| `AuthenticateLogin.cs` | Core auth model in Definitions |
| `FileStationOperationResponses.cs` | Generic grouping, not specific |

### 3. **Root-Level Files Without Clear Organization**

```
Data/
├── FsEntry.cs                    ← File system model (where does this belong?)
├── IGenericCloneable.cs           ← Interface (should be in Interfaces/)
└── LicenseInfo.cs                 ← Domain model (belongs in domain folder)
```

### 4. **Parameters Folder Structure is API-Centric, Not Operation-Centric**

Current: Grouped by API name (`FileStationAPI/`, `CoreAclAPI/`)  
Problem: Each file contains multiple related parameter classes (Start, Status, Stop) that should be together

---

## Proposed New Structure

```
Askyl.Dsm.WebHosting.Data/
├── DsmApi/                          ← External Synology DSM APIs (renamed from API/)
│   ├── Models/                      ← Request/response models (renamed from Definitions/)
│   │   ├── Core/
│   │   │   ├── AuthenticateLogin.cs
│   │   │   └── ApiInformationQuery.cs
│   │   ├── FileStation/             ← All FileStation data models
│   │   │   ├── FileStationCompress.cs
│   │   │   ├── FileStationFile.cs (extracted from responses)
│   │   │   └── ...
│   │   └── ReverseProxy/            ← Reverse proxy models
│   │       └── ...
│   ├── Parameters/                  ← API call parameters (reorganized)
│   │   ├── Core/                    ← Grouped by domain, not API name
│   │   │   ├── AuthenticationParameters.cs (merged login params)
│   │   │   └── SystemInformationParameters.cs
│   │   ├── FileStation/             ← All FileStation operations
│   │   │   ├── BackgroundTaskParameters.cs
│   │   │   ├── CompressParameters.cs (Start/Status/Stop in one file)
│   │   │   ├── CopyMoveParameters.cs
│   │   │   └── ...
│   │   └── ReverseProxy/            ← Reverse proxy operations
│   │       └── ...
│   └── Responses/                   ← API response wrappers (keep as-is)
│       ├── FileStationResponses.cs (consolidated from multiple files)
│       └── ...
├── Domain/                          ← Application domain models (NEW)
│   ├── FileSystem/
│   │   └── FsEntry.cs               ← Moved from root
│   ├── Licensing/
│   │   └── LicenseInfo.cs           ← Moved from root
│   └── WebSites/                    ← Already exists, keep
│       ├── WebSiteConfiguration.cs
│       └── WebSiteInstance.cs
├── Interfaces/                      ← Cross-cutting interfaces (NEW)
│   └── IGenericCloneable.cs         ← Moved from root
├── Results/                         ← Application-level result models (keep)
│   ├── DirectoryFilesResult.cs
│   └── ...
├── Security/                        ← Security-related models (keep)
│   └── LoginModel.cs
├── Services/                        ← Service interfaces (keep)
│   ├── IAuthenticationService.cs
│   └── ...
├── Runtime/                         ← .NET runtime models (keep)
│   ├── AspNetChannel.cs
│   └── ...
├── Attributes/                      ← Custom attributes (keep)
├── Exceptions/                      ← Custom exceptions (keep)
└── Extensions/                      ← Extension methods (keep)
```

---

## Detailed Changes

### 1. **Rename `API/` → `DsmApi/`**

**Rationale:** Clear distinction between external Synology APIs and internal application code.

| Old Path | New Path |
|----------|----------|
| `Data/API/Definitions/` | `Data/DsmApi/Models/` |
| `Data/API/Parameters/FileStationAPI/` | `Data/DsmApi/Parameters/FileStation/` |
| `Data/API/Responses/` | `Data/DsmApi/Responses/` |

**Impact:** 120+ files renamed/moved

### 2. **Consolidate Parameter Files by Operation**

**Current Problem:** Each FileStation operation has parameters scattered across API-specific folders.

**Example - Compress Operation:**

```csharp
// Current: FileStationAPI/FileStationCompressParameters.cs
public class FileStationCompressStartParameters(...) : ApiParametersBase<FileStationCompress> { }
public class FileStationCompressStatusParameters(...) : ApiParametersBase<ApiParametersNone> { }
public class FileStationCompressStopParameters(...) : ApiParametersBase<ApiParametersNone> { }

// Proposed: Keep together but rename folder structure
// DsmApi/Parameters/FileStation/CompressParameters.cs (same content, better location)
```

**Benefit:** Clearer that these three classes are related operations on the same resource.

### 3. **Remove Empty `Requests/` Folder**

The `API/Requests/` folder is empty and serves no purpose. Remove it.

### 4. **Move Root-Level Files to Appropriate Folders**

| File | Current Location | Proposed Location | Rationale |
|------|-----------------|-------------------|-----------|
| `FsEntry.cs` | Root | `Domain/FileSystem/FsEntry.cs` | File system domain model |
| `LicenseInfo.cs` | Root | `Domain/Licensing/LicenseInfo.cs` | Licensing domain model |
| `IGenericCloneable.cs` | Root | `Interfaces/IGenericCloneable.cs` | Cross-cutting interface |

### 5. **Consolidate Response Files**

**Current:** Multiple response files with few classes each:
- `FileStationOperationResponses.cs` (4 classes)
- `FileStationListResponse.cs` (1 class)
- `FileStationSearchResponse.cs` (2 classes)

**Proposed:** Group by operation type:
```
DsmApi/Responses/
├── FileStation/
│   ├── FileOperationResponses.cs    ← create, rename, delete, copy/move
│   ├── ListResponses.cs             ← list, list_share
│   ├── SearchResponses.cs           ← search results
│   └── TaskResponses.cs             ← background tasks, compress, extract
├── Core/
│   └── AuthenticationResponses.cs   ← login responses
└── ReverseProxy/
    └── ProxyResponses.cs            ← reverse proxy operations
```

### 6. **Rename `Definitions/` → `Models/`**

**Rationale:** "Definitions" is vague. These are clearly data models for DSM API requests/responses.

---

## Namespace Changes

| Old Namespace | New Namespace |
|--------------|---------------|
| `Data.API.Definitions.Core` | `Data.DsmApi.Models.Core` |
| `Data.API.Definitions.FileStation` | `Data.DsmApi.Models.FileStation` |
| `Data.API.Parameters.FileStationAPI` | `Data.DsmApi.Parameters.FileStation` |
| `Data.API.Parameters.AuthenticationAPI` | `Data.DsmApi.Parameters.Core` |
| `Data.API.Responses` | `Data.DsmApi.Responses` (or sub-namespaces) |

---

## Migration Strategy

### Phase 1: Structural Changes (No Logic Changes)

1. **Rename folders:**
   - `API/` → `DsmApi/`
   - `Definitions/` → `Models/`
   - Remove empty `Requests/`

2. **Reorganize Parameters folder:**
   - Flatten `FileStationAPI/` → `FileStation/`
   - Flatten `AuthenticationAPI/` → move to `Core/`
   - Flatten `InformationsAPI/` → move to `Core/`
   - Keep `ReverseProxyAPI/` → `ReverseProxy/`

3. **Move root files:**
   - Create `Domain/FileSystem/`, `Domain/Licensing/`, `Interfaces/`
   - Move individual files

### Phase 2: Response Consolidation (Logic Changes)

1. Group response classes by operation type
2. Update all references across codebase
3. Verify build success

### Phase 3: Cleanup & Verification

1. Update all using directives across solution
2. Run full build and verify no warnings/errors
3. Update documentation references

---

## Impact Analysis

### Files Affected

**Within Data Project:**
- ~120 files moved/renamed (DsmApi folder reorganization)
- ~3 files moved from root to Domain/Interfaces
- ~8 response files consolidated (potential logic changes)

**Across Solution (References Updated):**
Based on previous Constants refactoring pattern, expect:
- `Ui/Controllers/*` - Update using directives
- `Ui/Services/*` - Update using directives
- `Ui.Client/Services/*` - Update using directives
- `Tools/*` - Update using directives

**Estimated Total:** 150+ files modified across solution

### Build Impact

All changes are **refactoring-only** (no logic changes in Phase 1):
- Same functionality
- Same public APIs (namespace changes only)
- Build should succeed immediately after reference updates

---

## Benefits

### 1. **Clear Separation of Concerns**

```
Before: Data/API/ - What does "API" mean here?
After: 
  - DsmApi/ - External Synology APIs (clear!)
  - Domain/ - Application domain models (clear!)
```

### 2. **Better Discoverability**

Developers can quickly find:
- DSM API models → `DsmApi/Models/`
- API call parameters → `DsmApi/Parameters/`
- Domain entities → `Domain/`
- Cross-cutting interfaces → `Interfaces/`

### 3. **Consistent with Constants Project**

After the Constants reorganization, we now have:
```
Constants/
├── DSM/           ← External Synology constants
└── WebApi/        ← Internal routes

Data/
├── DsmApi/        ← External Synology data models
└── Domain/        ← Internal domain models
```

**Parallel structure makes the codebase more intuitive!**

### 4. **Scalability**

New DSM APIs can be added to `DsmApi/` without confusion about where they belong.

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking changes in namespaces | High | Update all references systematically, verify build |
| Missing a file reference | Medium | Use grep search to find all usages before/after |
| Response consolidation bugs | Medium | Keep Phase 1 & 2 separate, test thoroughly |
| Merge conflicts if parallel work | Low | Complete reorganization in single PR |

---

## Recommendations

### ✅ **Recommended: Proceed with Full Reorganization**

**Rationale:**
1. Matches the Constants project cleanup already completed
2. Improves code clarity and maintainability significantly
3. All changes are mechanical (find/replace namespaces) - low risk
4. Build verification ensures nothing breaks

### 📋 **Suggested Approach**

1. **Create detailed task list** from this proposal
2. **Execute Phase 1 first** (structural only, no logic changes)
3. **Verify build success** after Phase 1
4. **Commit Phase 1** as "refactor: Reorganize Data project structure"
5. **Execute Phase 2** (response consolidation) if time permits
6. **Final verification and commit**

---

## Next Steps

If you approve this proposal, I will:

1. Create a detailed todo list with all file movements
2. Execute the reorganization systematically
3. Update all references across the solution
4. Verify build success at each phase
5. Commit changes with clear commit messages

**Estimated effort:** 2-3 hours for complete reorganization and verification

---

*Generated by Qwen Code on March 22, 2026*  
*Proposal for Data project reorganization to match Constants project improvements*

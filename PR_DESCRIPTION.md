# feat: Complete UI migration to client-server architecture with .NET 10 upgrade and service layer refactoring

## 🎯 Overview

This PR completes the major architectural migration from the legacy Uiz-Old project to the new client-server architecture (Ui + Ui.Client), along with comprehensive service layer refactoring, code standardization, and .NET 10 upgrade.

## 📋 Key Changes

### 1. **UI Architecture Migration**
- ✅ Migrated all UI components from `Uiz-Old` to new `Ui` (server) and `Ui.Client` (WASM) projects
- ✅ Implemented client-server separation with REST API communication pattern
- ✅ Removed legacy Uiz-Old project entirely after successful migration
- ✅ Updated FluentUI components throughout the application

### 2. **.NET 10 & C# 14 Upgrade**
- ✅ Upgraded solution to .NET 10 runtime
- ✅ Adopted C# 14 language features (primary constructors, collection expressions, GeneratedRegexAttribute)
- ✅ Updated all project files and dependencies for .NET 10 compatibility
- ✅ Leveraged new language features for cleaner, more idiomatic code

### 3. **Authentication & Security**
- ✅ Implemented session-based authentication flow
- ✅ Added temporary token service for secure login
- ✅ Created `AuthorizeSessionAttribute` for protected endpoints
- ✅ Integrated authentication UI with server-side validation

### 4. **Service Layer Refactoring**
- ✅ Centralized JSON serialization options in `JsonOptionsCache`
- ✅ Implemented standardized `ApiResult<T>` pattern architecture
- ✅ Created derived result types: `ApiResultBool`, `ApiResultData<T>`, `ApiResultItems<T>`, `ApiResultValue<T>`
- ✅ Migrated reverse proxy operations to server-side `WebSiteHostingService`
- ✅ Added HttpClient extension methods for consistent API communication

### 5. **Feature Implementations**
- ✅ **Runtime Management:** Complete .NET framework installation and version detection
- ✅ **License Feature:** Parallel loading with IWorkingState pattern, licenses dialog
- ✅ **Log Download:** Session-based authentication for log retrieval
- ✅ **File System:** Enhanced file selection dialog with tree navigation
- ✅ **Home Page:** Comprehensive dashboard with system metrics and real-time updates

### 6. **Code Quality & Standards**
- ✅ Centralized all magic strings/numbers in `Askyl.Dsm.WebHosting.Constants` project
- ✅ Standardized build/clean commands (`dotnet build /nr:false ./src/*.slnx`)
- ✅ Enforced String/string pattern (PascalCase for static, lowercase for types)
- ✅ Updated QWEN.md with comprehensive coding standards and AI assistant guidelines
- ✅ Fixed all French text to English throughout the codebase

### 7. **Developer Experience**
- ✅ Added benchmark project with UriBuilder performance tests
- ✅ Created comprehensive migration documentation in `docs/ai/Migration.md`
- ✅ Updated technical architecture documentation
- ✅ Improved error handling with custom exceptions (e.g., `ReverseProxyNotFoundException`)

## 🔧 Technical Details

### New Project Structure
```
src/
├── Askyl.Dsm.WebHosting.Constants/     # Centralized constants
├── Askyl.Dsm.WebHosting.Data/          # Shared data models & interfaces
├── Askyl.Dsm.WebHosting.Tools/         # Shared utilities (HttpClient, DsmApiClient)
├── Askyl.Dsm.WebHosting.Ui/            # Server-side Blazor (Controllers + Services)
├── Askyl.Dsm.WebHosting.Ui.Client/     # Client-side Blazor WASM (Components + Interfaces)
└── Askyl.Dsm.WebHosting.Benchmarks/    # Performance benchmarks
```

### Migration Path Completed
1. ✅ Created data contracts for Uiz-Old service interfaces
2. ✅ Implemented client-server communication layer
3. ✅ Migrated all features incrementally (auth → runtime → licenses → files)
4. ✅ Removed legacy code and cleaned up obsolete files

## 🧪 Testing Recommendations

- [ ] Test authentication flow (login/logout/session expiry)
- [ ] Verify .NET framework installation workflow
- [ ] Check log download functionality with session auth
- [ ] Validate file selection dialog navigation
- [ ] Review home page metrics and real-time updates
- [ ] Ensure reverse proxy creation/modification/deletion works correctly

## 📚 Documentation Updates

- `docs/ai/Migration.md` - Complete migration guide and checklist
- `docs/ai/technical-architecture.md` - Updated architecture overview
- `QWEN.md` - Comprehensive coding standards for AI-assisted development

## ⚠️ Breaking Changes

- **Removed:** Uiz-Old project and all related files
- **Changed:** Result types now follow `ApiResult<T>` hierarchy
- **Updated:** All service interfaces moved to `Data/Services` folder
- **Modified:** Build command requires `/nr:false` flag

## 🚀 Next Steps

1. Review and merge this PR
2. Update deployment scripts if needed
3. Consider performance testing with benchmarks
4. Plan for additional .NET 10/C# 14 feature adoption

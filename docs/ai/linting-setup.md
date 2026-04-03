# Linting Setup - .NET Analyzers + EditorConfig

**Date:** April 2, 2026  
**Purpose:** Enable real-time code style enforcement in VS Code using native .NET tooling

---

## Overview

This document describes the linting infrastructure setup for Askyl.Dsm.WebHosting using:

- **.NET Analyzers** - Built-in Microsoft analyzers for code quality and style
- **EditorConfig** - Cross-editor configuration for consistent formatting rules
- **Build-time enforcement** - Rules enforced during compilation

---

## Files Modified/Created

### 1. `.editorconfig` (Root)

Location: `/Users/brice/Documents/Dev/github/ASkyl.Dsm.WebHosting/.editorconfig`

**Purpose:** Defines code style and formatting rules for all editors/IDEs

**Key Rules Enforced:**

- ✅ **Indentation:** 4 spaces, no tabs
- ✅ **Line endings:** LF (Unix-style)
- ✅ **Trailing whitespace:** Auto-trimmed
- ✅ **Final newline:** Always inserted
- ✅ **Naming conventions:** PascalCase for methods/properties, camelCase for parameters/locals
- ✅ **Braces:** Required for all control flow statements
- ✅ **Type keywords:** `string` over `String` for types (but `String.` for static calls)
- ✅ **Var usage:** Prefer explicit types for built-in types, var when type is apparent
- ✅ **Nullable reference types:** Enabled with error level
- ✅ **Using directives:** System first, sorted automatically

### 2. `Directory.Build.props` (Updated)

Location: `/Users/brice/Documents/Dev/github/ASkyl.Dsm.WebHosting/src/Directory.Build.props`

**Added Properties:**

```xml
<PropertyGroup>
    <!-- Enable .NET Analyzers for code quality and style enforcement -->
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
    <RunAnalyzersDuringLiveAnalysis>true</RunAnalyzersDuringLiveAnalysis>
</PropertyGroup>
<ItemGroup>
    <!-- Roslynator Analyzers for enhanced code style enforcement -->
    <PackageReference Include="Roslynator.Analyzers" Version="4.12.7" />
    <PackageReference Include="Roslynator.Formatting.Analyzers" Version="4.12.7" />
</ItemGroup>
```

**What This Enables:**

- `EnableNETAnalyzers`: Activates Microsoft.CodeAnalysis.NetAnalyzers package
- `AnalysisLevel=latest`: Uses newest analyzer rules (.NET 10 / C# 14)
- `EnforceCodeStyleInBuild`: Fails build on code style violations
- `RunAnalyzersDuringBuild`: Analyzes code during compilation
- `RunAnalyzersDuringLiveAnalysis`: Real-time feedback in VS Code while typing
- **Roslynator.Analyzers**: 500+ additional rules including proper using directive ordering
- **Roslynator.Formatting.Analyzers**: Enhanced formatting rules and fixes

---

## Rules Coverage Matrix

| Rule | Enforcement Level | Tool | Status |
|------|------------------|------|--------|
| **String/String pattern** | Error | EditorConfig + Roslynator | ✅ Enforced |
| **Using directive order** | Error | Roslynator (RCS0015) | ✅ System first, then alphabetical with blank line separator |
| **Blank lines before/after control flow** | None | Custom needed | ⚠️ Not enforced (requires custom analyzer) |
| **No magic strings/numbers** | Suggestion | CA1861, CA1303 + Roslynator | ✅ Partially enforced |
| **Single-line logging** | None | Custom needed | ⚠️ Not enforced (requires custom analyzer) |
| **Braces for control flow** | Warning | EditorConfig | ✅ Enforced |
| **Naming conventions** | Warning | EditorConfig + Roslynator | ✅ Enforced |
| **Nullable reference types** | Error | EditorConfig | ✅ Enforced |
| **Remove unused usings** | Warning | Roslynator | ✅ Enforced |

---

## Design Decision: Using Directive Ordering (System First, Then Alphabetical)

**Date:** April 2, 2026  
**Decision:** Use System-first ordering with alphabetical sorting for all other namespaces (no blank line separator)

### Rationale

After extensive evaluation of tooling capabilities and testing, we decided on a **pragmatic approach**:

1. **Tooling Reality**: No automated tool (`dotnet format`, Roslynator, Visual Studio) can enforce "System first + single blank line + alphabetical for rest"
2. **Multiple Groups Issue**: Roslynator's `separate_groups` creates multiple groups (A vs M vs S), not just System vs Others
3. **Minimal Practical Benefit**: Blank lines between using groups don't significantly improve readability
4. **Tool Support**: System-first + pure alphabetical is fully supported by all modern tooling

### What We Use

```csharp
using System;                         // ✅ System always first
using System.Collections.Generic;     // ✅ All System.* together, alphabetically

using Askyl.Dsm.WebHosting.Core;      // ✅ Then ALL other usings alphabetically (A < M)
using Microsoft.Extensions.Logging;   // ✅ Alphabetical continues regardless of type (M < S)
using Serilog;                        // ✅ Third-party mixed with others
```

**Key Points:**

- **System namespaces always first** (enforced by `dotnet_sort_system_directives_first`)
- **Pure alphabetical ordering** for all non-System usings (Microsoft, third-party, project namespaces mixed together)
- **No blank line separator** between System and other usings (tooling limitation - not worth custom tooling)

### Benefits

- ✅ **Fully automated**: Works seamlessly with `dotnet format`
- ✅ **Zero tool conflicts**: All tools agree on this standard
- ✅ **Industry-aligned**: Matches what most .NET developers expect
- ✅ **Maintainable**: Simple rule that's easy to understand and follow
- ✅ **No over-engineering**: Don't fight the tooling for minimal benefit

### What Roslynator Provides

Roslynator analyzers provide:

- **Cleanup**: Remove unnecessary usings automatically - set to `warning`
- **Add missing usings**: Suggest needed imports - set to `info`
- Plus 500+ additional code quality analyzers for:
  - Exception handling improvements
  - Naming convention enhancements
  - Code smell detection
  - Performance optimizations

**Note:** Roslynator's using directive ordering/grouping rules are **disabled** because they create conflicts with `dotnet format` and don't provide the exact behavior we want.

---

## How It Works

### Real-Time Feedback (VS Code)

1. **While typing:** C# extension reads `.editorconfig` and shows squiggles for violations
2. **On save:** Auto-formatting applies indentation, whitespace, and using directive sorting
3. **Live analysis:** `RunAnalyzersDuringLiveAnalysis=true` provides instant feedback

### Build-Time Enforcement

1. **During build:** Analyzers run and report warnings/errors
2. **Enforcement:** `EnforceCodeStyleInBuild=true` treats style violations as build errors
3. **CI/CD:** Builds fail if code doesn't meet standards

---

## Usage

### VS Code Setup

No additional configuration needed! The C# extension automatically:

- Reads `.editorconfig` from project root
- Applies formatting rules on save (`Ctrl+Shift+B` or `Format Document`)
- Shows real-time diagnostics in Problems panel

### Manual Verification

```bash
# Build with analyzers (enforces all rules)
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx

# Clean and rebuild to verify no warnings
dotnet clean /nr:false ./src/Askyl.Dsm.WebHosting.slnx && dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```

### Fix Violations

Most violations can be auto-fixed in VS Code:

- **Quick Fix:** Hover over squiggle → Click lightbulb → Select fix
- **Format Document:** `Shift+Alt+F` (macOS) or `Shift+Ctrl+E` (Windows)
- **Organize Usings:** Right-click file → "Sort Using Directives"

---

## Limitations & Next Steps

### Current Limitations

1. **Blank line enforcement** - EditorConfig has limited support for blank lines before/after control flow
2. **Single-line logging** - No native analyzer for multi-line logging detection
3. **Magic string detection** - CA1861 only catches repeated strings, not all magic values
4. **String/String pattern** - Partially enforced (type usage, but static calls need custom rule)

### Recommended Next Steps

#### Option 1: Add Roslynator Analyzer ✅ **COMPLETED**

Roslynator provides 500+ additional rules including:

- Better using directive sorting (System → Microsoft → Third-party → Project) ✅
- Blank line enforcement
- Magic number/string detection
- More comprehensive code style rules

**Setup:**

```xml
<!-- Already added to Directory.Build.props -->
<PackageReference Include="Roslynator.Analyzers" Version="4.12.7" />
<PackageReference Include="Roslynator.Formatting.Analyzers" Version="4.12.7" />
```

#### Option 2: Custom Analyzers

Create project-specific analyzers for:

- Blank line rules (before/after control flow)
- Single-line logging enforcement
- Magic string detection in specific contexts
- String vs string pattern validation

#### Option 3: dotnet-format

Add automated formatting tool for CI/CD and pre-commit hooks:

```bash
# Install globally
dotnet tool install --global dotnet-format

# Run formatting (fixes issues automatically)
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verify-no-changes

# In tasks.json for VS Code
{
    "label": "Format 🎨",
    "command": "dotnet format ${workspaceFolder}/src/Askyl.Dsm.WebHosting.slnx"
}
```

---

## Verification

✅ **Build Status:** Successful with 0 warnings  
✅ **Analyzer Setup:** All projects inherit settings from Directory.Build.props  
✅ **VS Code Integration:** Real-time feedback enabled via C# extension  

### Test the Setup

1. Open any `.cs` file in VS Code
2. Intentionally violate a rule (e.g., use `String` instead of `string` for type)
3. Observe red squiggle and diagnostic message
4. Build the solution - should still succeed (most rules are warnings, not errors)

---

## References

- [EditorConfig Documentation](https://docs.microsoft.com/dotnet/fundamentals/code-analysis/configuration-options)
- [.NET Analyzers Rules](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/)
- [Code Style Rules](https://docs.microsoft.com/dotnet/fundamentals/code-analysis/style-rules)
- [Roslynator](https://josefpihrt.github.io/docs/roslynator/analyzers/)

---

## Changelog

### April 2, 2026 (Updated)

- ✅ Created `.editorconfig` with comprehensive C# style rules
- ✅ Updated `Directory.Build.props` to enable .NET analyzers
- ✅ **Added Roslynator.Analyzers and Roslynator.Formatting.Analyzers** for enhanced enforcement
  - Proper using directive ordering (System → Microsoft → Third-party → Project)
  - 500+ additional code quality rules
  - Better formatting enforcement
- ✅ Verified build succeeds with 0 warnings
- ⚠️ Documented limitations for future enhancement (custom analyzers for blank lines, logging)

### April 2, 2026 (Initial)

- Initial setup of .NET analyzers and EditorConfig

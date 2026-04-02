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
```

**What This Enables:**
- `EnableNETAnalyzers`: Activates Microsoft.CodeAnalysis.NetAnalyzers package
- `AnalysisLevel=latest`: Uses newest analyzer rules (.NET 10 / C# 14)
- `EnforceCodeStyleInBuild`: Fails build on code style violations
- `RunAnalyzersDuringBuild`: Analyzes code during compilation
- `RunAnalyzersDuringLiveAnalysis`: Real-time feedback in VS Code while typing

---

## Rules Coverage Matrix

| Rule | Enforcement Level | Tool | Status |
|------|------------------|------|--------|
| **String/String pattern** | Error | EditorConfig | ✅ Enforced |
| **Using directive order** | Warning | EditorConfig | ✅ Enforced |
| **Blank lines before/after control flow** | None | Custom needed | ⚠️ Not enforced (requires custom analyzer) |
| **No magic strings/numbers** | Suggestion | CA1861, CA1303 | ✅ Partially enforced |
| **Single-line logging** | None | Custom needed | ⚠️ Not enforced (requires custom analyzer) |
| **Braces for control flow** | Warning | EditorConfig | ✅ Enforced |
| **Naming conventions** | Warning | EditorConfig | ✅ Enforced |
| **Nullable reference types** | Error | EditorConfig | ✅ Enforced |

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

#### Option 1: Add Roslynator Analyzer

Roslynator provides 500+ additional rules including:
- Better using directive sorting
- Blank line enforcement
- Magic number/string detection
- More comprehensive code style rules

**Setup:**
```xml
<!-- Add to each .csproj or create central package reference -->
<PackageReference Include="Roslynator.Analyzers" Version="4.12.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
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

### April 2, 2026
- ✅ Created `.editorconfig` with comprehensive C# style rules
- ✅ Updated `Directory.Build.props` to enable .NET analyzers
- ✅ Verified build succeeds with 0 warnings
- ⚠️ Documented limitations for future enhancement (Roslynator/custom analyzers)

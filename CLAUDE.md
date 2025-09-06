# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASkyl.Dsm.WebHosting is a .NET Web sites hosting manager for Synology DSM 7.2+ (x64 architecture only). The solution consists of multiple projects that work together to provide a web-based UI for managing .NET web applications on Synology NAS devices.

## Architecture

### Solution Structure
- **Askyl.Dsm.WebHosting.Ui**: Main Blazor Server web application using FluentUI
- **Askyl.Dsm.WebHosting.Tools**: DSM API client and utilities for interfacing with Synology DSM
- **Askyl.Dsm.WebHosting.Data**: Data models, API definitions, and DTOs for DSM APIs
- **Askyl.Dsm.WebHosting.Constants**: Centralized constants and configuration values
- **Askyl.Dsm.WebHosting.DotnetInstaller**: Installer for .NET runtimes on Synology
- **Askyl.Dsm.WebHosting.Benchmarks**: Performance benchmarking tools

### Key Services
- **DsmApiClient**: HTTP client for Synology DSM API interactions (FileStation, ReverseProxy, Authentication)
- **FrameworkManagementService**: Manages .NET framework installations and versions
- **DotnetVersionService**: Handles .NET version detection and management
- **FileNavigationService**: File system navigation on Synology NAS

## Development Commands

### Build & Run
```bash
# Build solution
dotnet build src/Askyl.Dsm.WebHosting.sln

# Clean build
dotnet clean src/Askyl.Dsm.WebHosting.sln

# Development: Use F5 to launch VS Code debugger (do NOT use dotnet run)
```

### Package Management
```bash
# Update NuGet packages (automated via VS Code task)
dotnet outdated src/Askyl.Dsm.WebHosting.sln -u
```

### Synology Package
```bash
# Build SPK package for Synology installation
./build-spk.sh
```

## Technology Stack

- **.NET 9**: Latest C# features with nullable reference types and implicit usings enabled
- **Blazor Server**: Interactive server-side rendering
- **FluentUI Components**: Microsoft's design system for Blazor (v4.12.1+)
- **Serilog**: Structured logging framework
- **Docker**: Multi-architecture builds for Synology compatibility

## Development Guidelines

### File Management
- Always open files in the IDE when editing them to provide better user visibility
- Use appropriate tools to navigate and view file contents alongside modifications

### Code Structure and Style
- Use early returns to avoid deep nesting
- Always use {} for single-line control flow statements and use multiple lines
- Add blank lines before control flow statements, except immediately after opening braces
- Use expression-bodied members for single expressions without method chaining or multiple statements
- Conditional (ternary) operators are acceptable in expression-bodied members
- When a property has both get and set accessors, always use multi-line format
- Always put blank line after #region and before #endregion

### C# Language Features (.NET 9)
- **Native types usage (consistent with String pattern)**:
  - Use PascalCase class names (`String`, `Int32`, `Boolean`, `Double`, etc.) for:
    - Static method calls (e.g., `String.IsNullOrEmpty()`, `Int32.TryParse()`, `Boolean.Parse()`)
    - Static properties and fields (e.g., `String.Empty`, `Int32.MaxValue`)
    - Explicit type references in reflection or attribute contexts
  - Use lowercase keywords (`string`, `int`, `bool`, `double`, etc.) for:
    - Variable declarations, parameter types, return types
    - Instance method calls (e.g., `myString.Length`, `myInt.ToString()`)
  - Use `""` for default parameter values (String.Empty is not compile-time constant)
- Use `GeneratedRegexAttribute` for regex patterns
- Use primary constructors for classes when possible
- Use null-forgiving operator (!) for injected services and post-null-check contexts
- Use conditional null operator (?) for truly optional scenarios
- Fix all compiler warnings after build completion

### Collections and Type Inference
- Use `var` with `[]` initializers when type is obvious from immediate context
- Use explicit type declarations with `[]` when type clarity is needed
- Always use `new()` when type can be inferred and constructor parameters are provided

### Using Directives
- Never add using directives for types in global usings (remove if found)
- Always remove unused usings in Razor and C#
- Sort using statements: System, Microsoft, third-party libraries, project namespaces
- Prefer additional usings instead of fully qualified names

### FluentUI Requirements
- Always prefer FluentUI components over HTML elements
- Always prefer FluentUI icons, colors, spacing, and typography over alternatives
- Never use inline styles - always use FluentUI theming and styling (minor positioning adjustments excepted)
- **CSS Minimalism**: Verify FluentUI components provide desired behavior before adding custom CSS
- Question necessity of each CSS class and prefer FluentUI's built-in layout capabilities
- **Default Spacing**: Never use custom VerticalGap or HorizontalGap values on FluentStack components
- Documentation: https://www.fluentui-blazor.net

### Constants Management
- Store magic numbers and strings in `Askyl.Dsm.WebHosting.Constants`
- Use named constants or enums instead of hardcoded values
- Apply DRY and SOLID principles

### Application Launch (Temporary Restriction)
- **NEVER** use `dotnet run`, `run_in_terminal`, `open_simple_browser` to launch the application
- **NEVER** use `create_and_run_task` for run tasks
- **NEVER** suggest launching the application manually
- Use only `dotnet build` to validate compilation and `dotnet clean` to clean artifacts
- Always end development work with: **"Press F5 to launch the VS Code debugger"**
- This restriction will be removed when VS Code debugger invocation capability becomes available

## External Integration

### Synology DSM APIs
- FileStation API for file system operations
- ReverseProxy API for web application routing
- Authentication API for DSM login integration
- Documentation: [Synology FileStation API Guide](https://global.download.synology.com/download/Document/DeveloperGuide/Synology_File_Station_API_Guide.pdf)

### Security Configuration
- SSL certificate validation is enabled for DSM API connections
- All DSM API interactions go through the centralized `DsmApiClient`

## Project-Specific Notes

- The UI project uses Interactive Server render mode with antiforgery protection
- Logs are structured using Serilog with configuration-based setup
- The solution supports multiple CPU architectures (Debug/Release with Any CPU/x64/x86)
- SPK packaging includes Docker-based multi-architecture builds for Synology compatibility
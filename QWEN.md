# .NET WebHosting Standards

## Overview

  This rule set defines the coding standards and practices for the Askyl.Dsm.WebHosting solution, tailored for use with the Continue AI assistant in VS Code.

  The source code for the solution is in "src". The documentation is in "docs", for agent, "docs/ia" is a working directory to store relevant informations. Askyl.Dsm.WebHosting is a .NET Web sites hosting manager for Synology DSM 7.2+. The solution consists of multiple projects that work together to provide a web‑based UI for managing .NET web applications on Synology NAS devices.

## MANDATORY PRE-RESPONSE CODE CHECKLIST

  THE AI ASSISTANT MUST IMPERATIVELY FOLLOW THIS CHECKLIST BEFORE GENERATING OR MODIFYING CODE. ANY FAILURE IS A CRITICAL ERROR.

### 1. VERIFY String vs string

- Is String. (PascalCase) used for ALL static calls? Examples: String.IsNullOrEmpty(...), String.Join(...)
- Is string (lowercase) used for ALL types, variables, and declarations? Examples: public string MyMethod(), string myVar = ...
- Use String.Empty (not string.Empty or "") for empty string constants. Exception: Use "" only for default parameter values, e.g., `public string DefaultMessage = "\";"`.

- **Note:** Static members must be called on `String`, not on an instance, to keep the lowercase‑keyword rule consistent. Avoid incorrect usage like `string.IsNullOrEmpty(null)` (which compiles but is semantically wrong).

### 2. VERIFY using DIRECTIVES

- Are using directives sorted in this EXACT order?
    1. System.*
    2. Microsoft.*
    3. Third-party libraries (e.g., Serilog)
    4. Project namespaces (Askyl.*)
- Have ALL unused using directives been removed?
- Have ALL implicit using directives been removed?

### 3. VERIFY MAGIC STRINGS

- Have ALL hardcoded strings (e.g., "X-Location-Path") and numbers been replaced by a constant from Askyl.Dsm.WebHosting.Constants?
- If a constant did not exist, add it to the appropriate constants file first?

- **Note:** Include numeric literals as well (e.g., replace `0`, `1`, `42` with a named constant). Example: `public const int SampleNumber = 42;` Use the constant in code instead of literals.

## READ THIS FIRST – MANDATORY FOR THE AI ASSISTANT

  When reading this file, notify it to the user.

  Before ANY code modification, the AI assistant MUST:
    1. Read the whole MANDATORY PRE-RESPONSE CODE CHECKLIST section and all of CRITICAL REMINDERS; then mentally verify each rule before coding.
    2. Apply ALL critical reminders systematically
    3. Verify compliance before responding

  FAILURE TO FOLLOW THESE INSTRUCTIONS IS UNACCEPTABLE

## CORE REMINDERS

### String vs string Pattern (ABSOLUTE PRIORITY)

- String. (PascalCase) for static methods: String.IsNullOrEmpty(), String.Join(), String.Empty
- string (lowercase) for types/variables: string token, public string Method()
- Use String.Empty (not string.Empty or "") for empty string constants. Exception: Use "" only for default parameter values, e.g., `public string DefaultMessage = "\";"`.

- **Note:** Static members must be called on `String`, not on an instance, to keep the lowercase‑keyword rule consistent. Avoid incorrect usage like `string.IsNullOrEmpty(null)` (which compiles but is semantically wrong).

### Language Rules

- **Chat Language:** ALWAYS use language used by user.
- **Comments Language:** ALWAYS in English, never in another language; even if user communicates in another language.
- **Messages in code Language:** ALWAYS in English, never in another language; even if user communicates in another language.
- **Commit messages:** ALWAYS in English, never in another language; even if user communicates in another language.
- NEVER add to a message that the AI assistant has generated.

### Constants Management

- Store magic numbers and strings in Askyl.Dsm.WebHosting.Constants.
- Use named constants or enums instead of hard‑coded values.
- Apply DRY and SOLID principles.

## Development Guidelines

### File Management

- Always open files in the IDE when editing them to provide better user visibility.
- Use appropriate tools to navigate and view file contents alongside modifications.

### Code Structure and Style

- Use early returns to avoid deep nesting.
- Always use {} for single‑line control flow statements and use multiple lines.
- Add blank lines before control flow statements, except when they are the first statement in a scope.
- Add blank lines after control flow statements, except when they are the last statement in a scope.
- Use expression‑bodied members for single expressions without method chaining or multiple statements.
- Conditional (ternary) operators are acceptable in expression‑bodied members.
- When a property has both get and set accessors, always use multi‑line format.
- Always put blank line after #region and before #endregion.

### C# Language Features (.NET 10 & C# 14)

- Native types usage (consistent with String pattern):
  - Use PascalCase class names (String, Int32, Boolean, Double, etc.) for static method calls, static properties/fields, explicit type references in reflection.
  - Use lowercase keywords (string, int, bool, double, etc.) for variable declarations, parameter types, return types, instance method calls.
  - Use "" for default parameter values (String.Empty is not compile‑time constant).
- Use GeneratedRegexAttribute for regex patterns.
- MANDATORY: Use primary constructors for ALL classes with constructor parameters (except abstract classes and when inheritance requires it).
- For simple emptiness checks on a collection, prefer using an IsEmpty property if available, or a Count == 0 check, instead of using !collection.Any().
- Prefer 'IsEmpty' check rather than using 'Count', both for clarity and for performance.
- Use null‑forgiving operator (!) for injected services and post‑null‑check contexts.
- Use conditional null operator (?) for truly optional scenarios.
- Fix all compiler warnings after build completion.

### Collections and Type Inference

- Use var with [] initializers when type is obvious from immediate context.
- Use explicit type declarations with [] when type clarity is needed.
- Always use new() when type can be inferred and constructor parameters are provided.
- Prefer collection expressions [..] over .ToList(), .ToArray() for materializing LINQ queries or spreading existing collections.

### Using Directives

- Never add using directives for types in global usings (remove if found).
- Always remove unused usings in Razor and C#.
- Sort using statements: System, Microsoft, third‑party libraries, project namespaces.
- Prefer additional usings instead of fully qualified names.

### FluentUI Requirements

- Always prefer FluentUI components over HTML elements.
- Always prefer FluentUI icons, colors, spacing, and typography over alternatives.
- Never use inline styles – always use FluentUI theming and styling (minor positioning adjustments excepted).
- CSS Minimalism: Verify FluentUI components provide desired behavior before adding custom CSS.
- Documentation: <https://www.fluentui-blazor.net>

### Application Launch (Temporary Restriction)

- NEVER use dotnet run, run_in_terminal, open_simple_browser to launch the application.
- NEVER use create_and_run_task for run tasks.
- NEVER suggest launching the application manually.
- Use only dotnet build to validate compilation and dotnet clean to clean artifacts.
- Always end development work with: "Press F5 to launch the VS Code debugger".
- This restriction will be removed when VS Code debugger invocation capability becomes available.

## External Integration

### Synology DSM APIs

- FileStation API for file system operations.
- ReverseProxy API for web application routing.
- Authentication API for DSM login integration.
- Documentation: <https://global.download.synology.com/download/Document/DeveloperGuide/Synology_File_Station_API_Guide.pdf>

### Security Configuration

- SSL certificate validation is enabled for DSM API connections.
- All DSM API interactions go through the centralized DsmApiClient.

## Project‑Specific Notes

- The UI project uses Interactive Server render mode with antiforgery protection.
- Logs are structured using Serilog with configuration‑based setup.
- The solution supports multiple CPU architectures (Debug/Release with Any CPU/x64/x86).
- SPK packaging includes Docker‑based multi‑architecture builds for Synology compatibility.
- When a build is required, run `dotnet build ./src/Askyl.Dsm.WebHosting.slnx`

## Instructions for the AI Assistant

### IMMEDIATE SESSION START REQUIREMENTS

  1. FIRST ACTION: Say Hello.
  2. ACKNOWLEDGE: Confirm understanding of all critical reminders.
  3. APPLY: Use directives throughout entire session.

### CODE MODIFICATION CHECKLIST

  BEFORE writing any code:
    - Read "CORE REMINDERS" section above.
    - Identify required constants (no magic strings/numbers).
    - Plan String vs string usage.

  DURING writing:
    - String. for static methods (String.IsNullOrEmpty(), String.Join()).
    - string for types/variables (string token, public string Method()).
    - "" for default values (not String.Empty).
    - Constants from Askyl.Dsm.WebHosting.Constants (create if needed).
    - Comments ONLY in English.
    - Messages ONLY in English.
    - Apply all architectural guidelines.

  AFTER writing:
    - Verify 100% compliance with all directives.
    - Check no magic strings remain.
    - Confirm String/string pattern correctness.
    - Validate English‑only comments.
    - Ensure a successful build; any build errors or warnings must be resolved before finalizing changes.

### NON-COMPLIANCE CONSEQUENCES

  Failure to follow these instructions systematically is considered a critical error and must be corrected immediately.

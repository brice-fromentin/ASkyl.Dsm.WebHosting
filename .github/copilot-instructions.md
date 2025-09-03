# Coding Standards and Guidelines

## General Directives
- Respond in the same language as the question, but generate all code, comments, and commit messages in English.
- Keep the code concise and relevant to the question and make it as simple as possible.
- Only suggest commits when explicitly requested by the user.

[]: # Do not suggest code that has been deleted.

## Code Structure and Style
- Use early returns to avoid deep nesting.
- Always use {} for single-line control flow statements and use multiple lines instead of only one.
- Add blank lines before control flow statements, except immediately after opening braces of methods, properties, or other scopes.
- Use expression-bodied members for methods, properties, and indexers only for simple single expressions (one property access, one method call, or simple arithmetic - no chaining, no complex logic, no conditions).
- When a property has both get and set accessors, always use a multi-line format.
- Always put a blank line after #region and before #endregion.

## Constants and Magic Values
- Avoid magic numbers, use named constants or enums instead in Askyl.Dsm.WebHosting.Constants.
- Avoid magic strings, use named constants or enums instead in Askyl.Dsm.WebHosting.Constants.

## Design Principles
- Apply DRY principles.
- Apply SOLID principles.

## C# Language Features
- We are targeting .NET 9, use the latest C# features.
- **String vs string usage**:
  - Use `String.Empty` (not `string.Empty` or `""`) for empty string constants
  - Use `String.IsNullOrEmpty()`, `String.IsNullOrWhiteSpace()` for static method calls
  - Use `string` keyword for variable declarations, parameter types, return types, local variables
  - **Exception**: Use `""` for default parameter values (String.Empty is not a compile-time constant)
  - **Memory aid**: Static members = `String.MethodName`, everything else = `string`
- Use GeneratedRegexAttribute for regex patterns.
- When possible use primary constructors for classes.
- Use null-forgiving operator (!) for injected services, post-null-check contexts, and component references after initialization. Use conditional null operator (?) for truly optional scenarios.
- Fix all compiler warnings after build completion.

## Collections and Type Inference
- For collections: Use `var` with `[]` initializers when the type is obvious from immediate context (return statements, assignments to typed variables, method parameters with explicit types).
- Use explicit type declarations with `[]` when type clarity is needed for readability or when the collection will be used across multiple statements.
- Always use `new()` when the type can be inferred and constructor parameters are provided (e.g., `new List<string>(capacity)`).

## Using Directives and Imports
- When the project supports implicit usings, never add using directives for types in the global usings, if you find one remove it.
- Always remove unused usings in Razor and C#.
- Sort using statements in this order: System namespaces, Microsoft namespaces, third-party libraries, project namespaces.
- Prefer additional usings instead of fully qualified names.

## FluentUI Guidelines
- FluentUI documentation : https://www.fluentui-blazor.net
- Always prefer FluentUI components over HTML elements.
- Always prefer FluentUI icons over other icon libraries.
- Always prefer FluentUI colors over custom colors.
- Always prefer FluentUI spacing over custom spacing.
- Always prefer FluentUI typography over custom typography.
- Never use inline styles, always use FluentUI theming and styling. Exception: minor positioning adjustments that cannot be achieved through FluentUI classes.
- **CSS Minimalism**: Before adding custom CSS classes, verify if FluentUI components already provide the desired behavior natively. Question the necessity of each CSS class and prefer FluentUI's built-in layout capabilities (FluentStack, FluentGrid, etc.) over custom styling.
- **Default Spacing**: Never use custom VerticalGap or HorizontalGap values on FluentStack components. Always rely on FluentUI's default spacing to maintain design system consistency.

## External APIs and Documentation
- Synology FileStation API documentation : https://global.download.synology.com/download/Document/DeveloperGuide/Synology_File_Station_API_Guide.pdf

## APPLICATION LAUNCH DIRECTIVE (Temporary)

**CONTEXT** : The assistant cannot currently invoke the VS Code debugger directly.

**TEMPORARY RESTRICTION** : While this limitation exists, apply the following rules:

### Strict prohibitions:
- Never use `dotnet run` or `run_in_terminal` to launch the application
- Never use `open_simple_browser` 
- Never suggest launching the application manually
- Never use `create_and_run_task` for run tasks

### Allowed actions:
- Use only `dotnet build` to validate compilation
- Use `dotnet clean` to clean build artifacts
- Modify, create, edit code and configuration files
- Manage NuGet packages and dependencies

### Required behavior at end of intervention:
- Always end with: **"Press F5 to launch the VS Code debugger"**
- The user has full control over launching via their debug environment

### Lift condition:
This directive will be removed when the assistant has a tool to directly invoke the VS Code debugger.

### Proactive notification requirement:
When debugger invocation capability becomes available, the assistant must immediately notify the user with: **"I can now launch the VS Code debugger directly. This directive has been automatically lifted."**
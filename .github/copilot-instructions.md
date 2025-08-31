# Coding Standards and Guidelines

## General Directives
- Respond in the same language as the question, but generate all code, comments, and commit messages in English.
- Keep the code concise and relevant to the question and make it as simple as possible.
- Never suggest to commit.

[]: # Do not suggest code that has been deleted.

## Code Structure and Style
- Use early returns to avoid deep nesting.
- Always use {} for single-line control flow statements and use multiple lines instead of only one.
- Add blank lines before control flow statements, except immediately after opening braces of methods, properties, or other scopes.
- Use expression-bodied members for methods, properties, and indexers only for simple single expressions (no chaining, no complex logic).
- When a property has both get and set accessors, always use a multi-line format.

## Design Principles
- Apply DRY principles.
- Apply SOLID principles.

## C# Language Features
- When calling static methods, properties, or fields of the String class, use the fully qualified String type (e.g., String.IsNullOrEmpty, String.Empty). For all other uses (variable declarations, parameter types, return types, etc.), use the native string keyword.
- For collections: Use `var` with `[]` initializers when the type is clear from context, explicit type declarations with `[]` when clarity is needed, `new()` for explicit constructor calls with parameters.
- Always use new() when the type can be inferred and constructor parameters are provided.
- Use GeneratedRegexAttribute for regex patterns.
- When possible use primary constructors for classes.
- Use null-forgiving operator (!) for injected services, post-null-check contexts, and component references after initialization. Use conditional null operator (?) for truly optional scenarios.
- Fix all compiler warnings after build completion.

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
- Never use inline styles, always use FluentUI theming and styling.

## External APIs and Documentation
- Synology FileStation API documentation : https://global.download.synology.com/download/Document/DeveloperGuide/Synology_File_Station_API_Guide.pdf
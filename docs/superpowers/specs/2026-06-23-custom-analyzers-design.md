# Custom Roslyn Analyzers Design

> **Status:** Approved
> **Date:** 2026-06-23
> **Branch:** `fix/visual-and-technical-fixes`

## Goal

Enforce AGENTS.md manual check rules at build time via custom Roslyn analyzers that emit errors, eliminating reliance on human review for:

1. Control flow blank lines
2. String/String static member pattern
3. Logger direct call compliance

## Architecture

Single analyzer project: `src/Askyl.Dsm.WebHosting.Analyzers/`

Three analyzers, each targeting a specific rule:

| Analyzer | ID | Rule | Auto-Fix |
|----------|-----|------|----------|
| `BlankLineAnalyzer` | ADWH01001, ADWH01002 | Blank lines before/after control flow | Yes (add) |
| `StringStaticMemberAnalyzer` | ADWH02001 | `String.` for static members, `string` for types | Yes (replace) |
| `LoggerDirectCallAnalyzer` | ADWH03001 | No direct `logger.LogXxx()` calls | No (developer must use `[LoggerMessage]`) |

All diagnostics are **errors** (build-blocking).

## Components

### BlankLineAnalyzer (ADWH01001/01002)

**Rules:**

- Blank line required before `if`, `else`, `foreach`, `for`, `while`, `switch`, `try`, `catch` — unless first in scope or preceded by a comment
- Blank line required after closing `}` of control flow — unless last in parent scope

**Sub-codes:**

- `ADWH01001` — missing blank line before control flow
- `ADWH01002` — missing blank line after control flow

**Implementation:** `SyntaxNodeAction` on `Block`, `IfStatement`, `WhileStatement`, `ForEachStatement`, `ForStatement`, `SwitchStatement`, `TryStatement`. Use trivia analysis to check blank lines.

### StringStaticMemberAnalyzer (ADWH02001)

**Rule:** Detect `string.Equals`, `string.IsNullOrWhiteSpace`, `string.IsNullOrEmpty`, `string.Empty`, `string.Create`, etc. used as static member calls (should use `String.` prefix).

**Implementation:** `SyntaxNodeAction` on `InvocationExpression` and `MemberAccessExpression`. Check if expression is `string.<StaticMember>`.

**Auto-fix:** Replace `string.` with `String.`.

### LoggerDirectCallAnalyzer (ADWH03001)

**Rule:** Detect direct calls to `logger.LogInformation()`, `logger.LogError()`, `logger.LogWarning()`, `logger.LogDebug()`, `logger.LogCritical()`, `logger.Log()`.

**Implementation:** `SyntaxNodeAction` on `InvocationExpression`. Check if method name matches `LogXxx` and receiver is named `logger` or implements `ILogger`.

**No auto-fix** — the fix requires adding a `[LoggerMessage]` attribute method, which is context-dependent.

## Project Structure

```text
src/Askyl.Dsm.WebHosting.Analyzers/
├── Askyl.Dsm.WebHosting.Analyzers.csproj
├── BlankLineAnalyzer.cs
├── BlankLineCodeFix.cs
├── StringStaticMemberAnalyzer.cs
├── StringStaticMemberCodeFix.cs
├── LoggerDirectCallAnalyzer.cs
├── Resources.Designer.cs
└── Resources.resx
```

## Distribution

Added to `Directory.Build.props`:

```xml
<ItemGroup>
    <ProjectReference Include="Askyl.Dsm.WebHosting.Analyzers/Askyl.Dsm.WebHosting.Analyzers.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Testing

Unit tests using `Microsoft.CodeAnalysis.Testing`:

- One test per rule (positive + negative cases)
- Test auto-fix produces correct output
- Test edge cases (first in scope, preceded by comment, last in scope)

## Exclusions

- Test projects: analyzers run but can be suppressed via `.editorconfig` if needed
- Generated code: excluded via `global::System.CodeDom.Compiler.GeneratedCodeAttribute`
- `.razor` files: analyzers run on generated C#, not on `.razor` directly

# .NET WebHosting Standards

## 1. PROJECT OVERVIEW

Askyl.Dsm.WebHosting is a .NET Web sites hosting manager for Synology DSM 7.2+.
The solution consists of multiple projects that work together to provide a web‑based
UI for managing .NET web applications on Synology NAS devices.

**Project Structure:**

- Source code: `src/`
- Agent documentation: `docs/ai/` (MUST place all AI-generated docs here)

---

## 2. ARCHITECTURE REFERENCE

**ALL architectural details are maintained in `docs/ai/technical-architecture.md`.** Consult this document before working on any feature.

---

## 3. DOCUMENTATION RULES

**ALL AI-generated documentation MUST be placed in `docs/ai/`.** When in doubt, use `docs/ai/`.

---

## 4. BUILD & FORMAT WORKFLOW

### Standardized Commands

```bash
dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet
dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx
dotnet clean /nr:false ./src/Askyl.Dsm.WebHosting.slnx
```

**NEVER** use `dotnet run` or variants without exact flags and solution path.

### Mandatory Sequence: Format → Build → Verify

1. **Format** — run format command above
2. **Build** — run build command above
3. **Verify** — ensure no errors or warnings

**NEVER skip the format step.**

### What `dotnet format` Enforces Automatically

- ✅ **Using directives**: System first, then alphabetical; removes unused usings
- ✅ **String/String pattern**: `string` for types/variables, `String.` for static members
- ✅ **Primary constructors**: Mandatory for classes with constructor parameters
- ✅ **Collection expressions**: `[..]` over `.ToList()`, `.ToArray()` (only when target type is inferable — `var` takes precedence)
- ✅ **Braces**: Always use `{}` for control flow statements
- ✅ **Blank lines**: After `#region`, before `#endregion`
- ✅ **Naming conventions**: PascalCase for properties/methods, camelCase for parameters/locals
- ✅ **Nullable reference types**: Enabled and enforced
- ✅ **All IDE0xxx/RCS0xxx/CAxxxx rules**

---

## 5. SESSION START PROTOCOL

The AI assistant MUST use an **inference-based approach** rather than hardcoded templates.

**Session Start Requirements (EXACT ORDER):**

1. **FIRST ACTION:** Say Hello briefly
2. **ACKNOWLEDGE:** List standards by extracting them from AGENTS.md (not hardcoded)
3. **DISPLAY MEMORIES:** Show all recorded memories from the memory system (loaded at session start)
4. **APPLY:** Use all extracted directives throughout the session
5. **DOCUMENTATION CHECK:** Before creating any docs, verify if they belong in `docs/ai/`

---

## 6. CODE STANDARDS

### 6.1 Language Rules

- **ALL** chat, comments, code messages, and commit messages: ALWAYS in English
- NEVER add to a message that the AI assistant has generated

### 6.2 C# Language Features (.NET 10 & C# 14)

**String vs string Pattern (CRITICAL):**

- `String.Equals`, `String.IsNullOrWhiteSpace`, `String.Empty`, `String.Format` — **ALWAYS** PascalCase `String.` for static members
- `string`, `int`, `bool`, `double` — **ALWAYS** lowercase for types, variables, parameters, return types
- Enforced by `StringStaticMemberAnalyzer` (ADWH02001) with auto-fix

```csharp
String.Equals(a, b, StringComparison.Ordinal)  // ✅ static method
String.IsNullOrWhiteSpace(input)              // ✅ static method
String.Empty                                    // ✅ static field
string name = "hello";                          // ✅ type declaration
string.Equals(a, b)    // ❌ NEVER
```

**Other Requirements:**

- Use `GeneratedRegexAttribute` for regex patterns
- **MANDATORY:** Use primary constructors for ALL classes with constructor parameters (except abstract classes and when inheritance requires it)
- **Collection Emptiness Checks:**
  - Non-null: `.Count == 0` or `.Length == 0`
  - Nullable inside block: `is { Count: > 0 }` — compiler knows it's not null inside
  - Avoid `?.Count > 0 == true` or `!.Any() == false`
- Use conditional null operator (`?`) for truly optional scenarios
- Fix all compiler warnings after build completion

### 6.3 Code Structure and Style

**General Principles:**

- Apply DRY and SOLID principles
- Use early returns to avoid deep nesting
- **Prefer simplicity** — Choose the simplest viable solution

**Method Declarations (MANUAL CHECK REQUIRED):**
Declarations with **≤ 4 parameters** on a single line unless total line length exceeds **200 characters**. Multi-line for >4 params regardless of length.

```csharp
public async Task<ApiResult> StopWebsiteAsync(Guid id)  // ✅ 3 params, single-line
public async Task<Result> CreateWebsiteAsync(  // ✅ 6 params, multi-line
    string name, Guid id, int port, string path, bool enableSsl, CancellationToken cancellationToken)
```

**Method Calls:**
Single-line for short calls. Multi-line for complex expressions with multiple parameters.

**Blank Line Rules (enforced by ADWH01001/01002):**

- Blank lines BEFORE/AFTER complete control structures (not first/last in scope)
- NO blank lines BETWEEN statements inside blocks
- Comments stay with their code — no blank line between comment and its statement

```csharp
// ✅ CORRECT
// This is an important check
if (condition)
{
    DoSomething();
    DoOtherThing();
}

DoNextThing();

// ❌ WRONG — blank line inside block
if (condition)
{
    DoSomething();

    DoOtherThing();
}

// ❌ WRONG — blank line between comment and code
// This check validates the input

if (condition)
{
    DoSomething();
}
```

**Additional Rules:**

- Use expression-bodied members for single expressions without method chaining
- Ternary operators acceptable in expression-bodied members
- Properties with both get/set: always multi-line format
- Blank line after `#region`, before `#endregion` (enforced)

### 6.4 Collections and Type Inference

- Use `var` with `[]` initializers when type is obvious from immediate context
- Use explicit types with `[]` when clarity is needed
- Always use `new()` when type can be inferred
- Prefer collection expressions `[..]` over `.ToList()`, `.ToArray()` — **only when target type is inferable**
- **Keep parameterized constructors on DTOs/records** when they enable one-line declarations

### 6.5 Constants Management

- Store magic numbers and strings in `Askyl.Dsm.WebHosting.Constants`
- Use named constants or enums instead of hard‑coded values
- If a constant does not exist, add it to the appropriate constants file first

### 6.6 Logging Standards

**ALL logging must use `[LoggerMessage]` source-generated extension methods.** Enforced by `LoggerDirectCallAnalyzer` (ADWH03001).

**Rules:**

- **No direct ILogger calls** — Never write `logger.LogInformation("...")`, `logger.LogError("...")`, etc.
- **Use extension methods** — `logger.LoginFailed(login)`
- **Specialized `ILogger<T>`** — Services inject `ILogger<ILogXxx>`, not bare `ILogger`
- **EventId assignment** — Consult `Constants/Logging/LogEventIds.cs` for range base
- **Extension file location** — `Askyl.Dsm.WebHosting.Logging/` — one file per service domain
- **XML doc comments** — Every `[LoggerMessage]` method must have `<summary>`

```csharp
logger.LoginFailed(login);           // ✅ extension method
logger.LogWarning("Login failed");   // ❌ direct ILogger call
```

**When adding new log methods:**

1. Identify service's EventId range in `Constants/Logging/LogEventIds.cs`
2. Find next available ID in corresponding extension file
3. Add `[LoggerMessage]` method with XML doc comment
4. Update `LogEventIds.cs` range comment if the range extends

---

## 7. COMPLIANCE ENFORCEMENT

### Tooling-Enforced (No Manual Check Required)

- **Using directives**: System first, alphabetical; unused removed
- **Primary constructors**: Mandatory for classes with constructor parameters
- **Collection expressions**: `[..]` over `.ToList()`, `.ToArray()` (when inferable)
- **String/String Pattern** (ADWH02001): `StringStaticMemberAnalyzer` with auto-fix
- **Logger Call Compliance** (ADWH03001): `LoggerDirectCallAnalyzer`
- **Control Flow Blank Lines** (ADWH01001/01002): `BlankLineAnalyzer` with auto-fix

### Manual Checks Required

1. **Magic Strings and Numbers** — replace ALL hardcoded strings/numbers with constants
2. **Target-Typed `new`** — use `new()` when type inferable (exception: variable name already includes type)
3. **Markdown Validation** — run `markdownlint <file-path>`, fix ALL errors

### Non-Negotiable Enforcement

After EVERY code modification:

1. **Format** → **Build** → **Fix issues** (including analyzer errors)
2. **Manual checks** — magic strings/numbers, target-typed `new`

---

## 8. PRE-RESPONSE CHECKLIST

### Before Writing Code

- [ ] Read "Compliance Enforcement" section
- [ ] Identify required constants (no magic strings/numbers)
- [ ] Identify correct `[LoggerMessage]` extension method (or plan new one)
- [ ] Review Git Safety Rules if git operations are needed

### During Writing

- [ ] Use constants from `Askyl.Dsm.WebHosting.Constants` (create if needed)
- [ ] Use `[LoggerMessage]` extension methods (no direct `ILogger` calls)
- [ ] Method declarations with ≤ 4 params on one line (unless > 200 chars)
- [ ] Comments/messages ONLY in English
- [ ] Apply architectural guidelines from `docs/ai/technical-architecture.md`
- [ ] Trust tooling for: String/String pattern, using directives, primary constructors, collection expressions, blank lines, logger calls

### After Writing

- [ ] Run `dotnet format` → `dotnet build /nr:false`
- [ ] Verify no magic strings/numbers remain (MANUAL)
- [ ] Verify method declarations with ≤ 4 params on one line (MANUAL)
- [ ] Validate English-only comments
- [ ] Ensure successful build with no errors or warnings
- [ ] Run `markdownlint <file-path>` for .md file changes
- [ ] FluentUI requirements met (for UI code)
- [ ] Application launch restrictions respected
- [ ] Documentation files placed in `docs/ai/` if AI-generated
- [ ] Git safety rules followed (if git operations involved)

---

## 9. EXTERNAL INTEGRATIONS

### Synology DSM APIs

- FileStation, ReverseProxy, Authentication APIs
- Documentation: <https://global.download.synology.com/download/Document/DeveloperGuide/Synology_File_Station_API_Guide.pdf>
- OSS Documentation: <https://github.com/pmilano1/synology-dsm-api/tree/master/docs/api-reference>
- SSL validation enabled; all interactions through `DsmApiClient`

**For API integration patterns, see `docs/ai/technical-architecture.md` section "Data Models & API Integration".**

---

## 10. FRAMEWORK REQUIREMENTS

### FluentUI Requirements

- Prefer FluentUI components, icons, colors, spacing, typography over alternatives
- No inline styles — use FluentUI theming (minor positioning excepted)
- CSS Minimalism: Verify FluentUI provides behavior before adding custom CSS
- Documentation: <https://www.fluentui-blazor.net>

**For component inventory, see `docs/ai/technical-architecture.md` section "UI Architecture".**

### Web Search Guidelines

**MANDATORY:** Perform web searches for potentially outdated information:

1. **.NET Updates** — new C# features, runtime updates, breaking changes, NuGet updates
2. **Third-Party Libraries** — releases, deprecations, security advisories
3. **Framework Updates** — Blazor/FluentUI, Serilog, Synology API changes
4. **Best Practices** — new C# 14+ patterns, security, performance

**Search Strategy:** Use `web-search` with specific queries; verify against official docs; cross-reference sources.

---

## 11. PROJECT-SPECIFIC NOTES

- UI uses Interactive Server render mode with antiforgery protection
- Logs structured using Serilog with configuration‑based setup
- Solution supports multiple CPU architectures (Any CPU/x64/x86)
- SPK packaging includes .NET multi‑architecture packages

**For detailed architecture, see `docs/ai/technical-architecture.md`.**

---

## 12. GIT SAFETY RULES (CRITICAL)

**NEVER execute dangerous git commands without explicit user confirmation.**

### Forbidden Without Explicit Authorization

- ❌ `git reset --hard`, `git reset --soft HEAD`
- ❌ `git clean -fd`, `git clean -ffdx`
- ❌ `git checkout -- .`
- ❌ `git rebase --abort`, `git reflog expire`, `git gc --prune=now`

### Required Safety Protocol

**BEFORE any state-modifying git command:**

1. **SHOW** the exact command
2. **EXPLAIN** impact
3. **GET** explicit confirmation
4. **RUN `git status`** first

### No Commits Without Authorization

- ❌ Never auto-commit after making changes
- ✅ MUST ask before committing
- ✅ MUST show proposed commit message, wait for approval
- ✅ MUST offer `git diff` review

**Correct workflow:** Make changes → format → build → ask user → show message → get approval → commit

### Commit Message Conventions

1. **NEVER** list changed files or include "Files Modified:" sections
2. **FOCUS** on "why" not "what"
3. **Use** conventional commit format: `type: description`
4. **Keep concise** — summary line (50 chars max), blank line, short bullets

```text
# ❌ WRONG
fix: HttpClient lifetime violation in LicenseService (Phase 5)

Files Modified:
- LicenseService.cs: Fixed lifetime

# ✅ CORRECT
fix: HttpClient lifetime violation in LicenseService (Phase 5)

Prevents socket exhaustion by using field-based HttpClient injection
instead of per-call disposal. Uses named client with configured
BaseAddress for /adwh sub path mapping.
```

### Safe Operations (No Confirmation Needed)

- ✅ `git status`, `git diff`, `git log`, `git branch`
- ✅ `git add <specific-file>`, `git commit -m "..."` (after showing message)

---

## 13. APPLICATION LAUNCH RESTRICTIONS (TEMPORARY)

- NEVER use `dotnet run`, `run_in_terminal`, or `open_simple_browser`
- NEVER use `create_and_run_task` for run tasks
- Use only standardized build/clean commands

---

## 14. EXECUTION ARCHITECTURE & SUB-AGENTS (SUPERPOWERS)

CRITICAL: To prevent VRAM saturation (PCIe swap) on the local host:

- STRICT SEQUENTIAL EXECUTION: exactly ONE tool, skill, or sub-agent per output turn
- WAIT FOR FEEDBACK: no guessing outputs
- AVOID PARALLEL ARRAYS: never group tool calls

---

## 15. CONTEXT COMPRESSION WARNING (CRITICAL)

**If you detect context compression or session state reset:**

1. **Re-read AGENTS.md immediately** — extract current standards dynamically
2. **Acknowledge ALL critical rules explicitly**
3. **Apply enforcement language strictly**
4. **Verify before responding** — Format → Build + manual checks

**DO NOT rely on memory from previous tasks.** Always re-read AGENTS.md when in doubt.

---

## 16. NON-COMPLIANCE CONSEQUENCES

Failure to follow these instructions systematically is a critical error and must be corrected immediately.

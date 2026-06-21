---
name: code-review-remediation
description: Workflow for addressing code review findings from a branch review document
source: auto-skill
extracted_at: '2026-06-17T14:00:00.000Z'
---

# Code Review Remediation Workflow

Trigger when the user asks to fix findings from a branch review document or address code review issues.

## Steps

1. **Read the review document** — extract all findings and their severity (Critical, High, Medium, Low, Nit)

2. **Cross-reference with codebase** — launch parallel exploration agents to verify which findings are already resolved and which are still open. Group by related files.

3. **Prioritize by severity** — Critical first, then High, then Medium. Skip findings already marked "by design" or "acceptable trade-off."

4. **Fix in batches** — group fixes by file to minimize format/build/test cycles. Apply all edits to a single file before moving to the next.

5. **Format → Build → Test after each batch** — never skip the verification cycle.

6. **Handle trial-and-error course corrections:**

   - **xUnit skip at runtime:** `SkipException` doesn't exist (MSTest only). `Assert.Skip()` conflicts with LINQ's `Skip`. Use `InvalidOperationException` with a clear message for precondition failures.
   - **Satellite assembly checks:** `GetResourceSet(culture, tryParents: false)` returns null even with a valid build. Use `GetManifestResourceNames().Any(n => n.Contains("SharedResource"))` instead for reliable manifest-level checks.
   - **DateTimeFormatInfo validation:** .NET 10 accepts virtually any string as a custom format pattern — `"%@!$"` doesn't throw `FormatException`. The catch blocks in format-setting code are defensive but unreachable. Test the `IsNullOrWhiteSpace` guard path instead.
   - **InternalsVisibleTo placement:** Must be in `<ItemGroup>`, not `<PropertyGroup>` in .NET SDK-style projects. Wrong placement causes MSB4066 error.
   - **Blazor WASM globalization with `autostart="false"`:** The MSBuild property `BlazorWebAssemblyLoadAllGlobalizationData=true` bundles globalization data at build time, but the `loadAllGlobalizationData: true` in `Blazor.start()` is what loads it at runtime. When `autostart="false"` is used, the `Blazor.start()` options are the sole runtime config — removing `loadAllGlobalizationData` from it breaks culture switching in the browser even though the data is bundled. They are complementary, not redundant.
   - **Missing translation fallback:** When changing `Localizer` fallback from bare key to bracketed `[{key}]`, also update `DeferredMessageExtensions.ResolveResource` and both `LocalizerTests` and `DeferredMessageExtensionsTests` assertions.
   - **init accessor with post-construction assignment:** Changing `{ get; set; }` to `{ get; init; }` on an interface property breaks code that sets it after DI resolves the service (e.g., `settings.SystemCulture = value` in middleware). Keep `{ get; set; }` and document single-use intent with XML comment instead.
   - **IJSRuntime in WASM for DOM updates:** When server-side Blazor renders `<html lang="...">` once at initial render, use `IJSRuntime.InvokeVoidAsync("document.documentElement.setAttribute", ...)` in the WASM `CultureManager.ApplyCulture` to update `lang` and `dir` attributes on culture change. Wrap in try/catch for `JSException` (DOM may not be ready during early startup).
   - **Debug.WriteLine for static utilities:** For static utility classes without `ILogger` access, use `Debug.WriteLine` for diagnostic output — visible in debug builds, stripped in release.
   - **Removing redundant record constructors:** When a record has both a parameterless constructor (for JSON deserialization) and an explicit parameterized constructor, the parameterized one is redundant — callers can use object initializer syntax instead. Update all call sites before removing.
   - **CultureManager test updates:** When adding `IJSRuntime` constructor parameter to `CultureManager`, all 19+ test instances need `Mock<IJSRuntime>` added. Use `CreateJsRuntimeMock()` helper and update both inline `new CultureManager(...)` calls and static init tests that use `CreateLoggerMock().Object` directly.

7. **Run manual compliance checklist** — after every code change, verify:
   - No `string.` for static members (use `String.`)
   - No magic strings/numbers
   - `[LoggerMessage]` extensions used (no direct `ILogger` calls)
   - Control flow blank lines correct
   - Target-typed `new` where applicable

8. **Report results** — summarize what was fixed, what remains open, and any findings that were reclassified (e.g., "unreachable code path", "by design").

## Notes

- When a finding names a specific file or function, verify it still exists before acting on it
- Resource completeness tests depend on satellite assemblies — add `[Trait("Category", "Integration")]` and a manifest-level precondition check
- xUnit collections (`[CollectionDefinition]` + `[Collection]`) serialize tests sharing thread-static state like `CurrentUICulture`
- Making static fields `internal` with `InternalsVisibleTo` enables direct test access (safe in WASM projects since test assembly isn't in browser build)
- Consolidate repetitive tests with `[Theory]` + `[InlineData]` to reduce test count while maintaining coverage
- Add reverse checks in completeness tests (e.g., orphaned resx keys not referenced by code) to catch dead translations

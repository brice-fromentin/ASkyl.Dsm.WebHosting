# FluentUI Blazor 5 Migration Analysis

Analysis of moving `Microsoft.FluentUI.AspNetCore.Components` from **4.14.5** to **5.0.1**, made on
2026-10-23 against `main` at `841d9a8`. Nothing has been migrated. Every statement is tagged with how it
was established:

- **measured** — a command was run and its output read;
- **read** — read in the 5.0.1 source (tag `v5.0.1`), its decompiled assembly, or the shipped assets;
- **upstream** — stated by the upstream documentation or issue tracker, not checked further;
- **inferred** — reasoning only. Nothing here was observed in a browser: running the application needs
  the maintainer's approval (AGENTS.md §13), and the visual checks in §10 are therefore theirs.

A compile spike exists on the **local, unpushed** branch `spike/fluentui-5` (`10d2a95`). It builds with zero
errors and zero warnings and passes the 592 tests. **It is not mergeable**: it carries the runtime defects
of §4, and that is the main finding of this analysis.

**Status: executed in 0.8.0.** The gates of §10.1 landed first, on 4.x. The spike was then applied on top of
them, and every defect of §4 was fixed against a test that failed first. §13 records what that run taught,
including where this analysis was wrong. What still needs a running application is in §10.3 and in the
tracker entry *FluentUI 5: what the migration leaves unverified*.

## 1. Verdict

- **This is a rewrite of every UI file, not a version bump.** All 14 `.razor` files that use FluentUI, the
  two tree helpers, two stylesheets and both project files. The compile-clean diff is 20 files, +353/−394,
  and §10.2 adds `Program.cs` on both sides.
- **The build cannot tell a finished migration from a broken one.** Removed components degrade to inert
  HTML elements with a *warning*, removed parameters degrade to HTML attributes with *nothing at all*, and
  CI does not fail on warnings (§3).
- **A green build still ships at least four defects** that neither the compiler nor the 592 tests see,
  two of them blocking: the login page throws on load, and the website dialog's number fields fetch code
  from `unpkg.com`, which the CSP refuses (§4).
- **Payload is not a concern**: about +25 KB compressed in total (§8).
- **Nothing forces the move today.** v4 is "no longer supported" but still maintained, with no end date
  (§9). v5 is four weeks past GA, with an accessibility batch filed against components we would use.

Recommendation: land the gates of §10.1 now, on 4.x, because they cost a line each and protect any
future UI change. Schedule the migration itself as the maintainer's call, not as a chore.

## 2. How it was measured

Repeatable without the spike:

1. **API diff.** Both assemblies were loaded with `System.Reflection.MetadataLoadContext`, every
   `[Parameter]` of every component and every enum dumped, then each `Fluent*` tag and attribute in our
   `.razor` files was checked against the 5.0.1 dump.
2. **Compile waves.** The package was bumped in a detached worktree and the solution rebuilt with the
   standard command after each round of fixes, until zero errors and zero warnings.
3. **Asset scans.** The shipped `lib.module.js` and CSS of both versions were searched for CSP-relevant
   constructs, CDN URLs and design tokens; the callers of each CDN loader were found by scanning the IL
   for `ldstr` of the JS function names.
4. **Publish.** `dotnet publish -c Release` (the `build-spk.sh` invocation) on both trees, sizes compared.
5. **Upstream.** The 5.0.1 nupkg, the docs sources and code at tag `v5.0.1`, the `archive-v4` README,
   NuGet registration data and the GitHub issue tracker.

## 3. Where the build is blind

### The compiler hides most errors behind the first ones

Measured. The tracker recorded "16 errors" for this bump. Those were declaration-stage only (`@inject`,
`@implements`, type arguments); the compiler stops before binding method bodies when declarations fail.

| Wave | Errors | Warnings |
|---|---|---|
| 1 — bump only | 16 | — |
| 2 — declarations fixed | 66 unique | ~30 RZ10012, 1 RZ2012, ~25 CS0618 |
| 3 | 1 | 0 |
| 4 | 0 | 0 |

### Removed components compile, as unknown HTML elements

Measured. Ten components we use no longer exist: `FluentDesignTheme`, `FluentMainLayout`, `FluentHeader`,
`FluentToolbar`, `FluentSplitter`, `FluentTextField`, `FluentNumberField`, `FluentDialogHeader`,
`FluentDialogFooter`, `FluentMenuProvider`. Each produces only **RZ10012**, a warning, and renders as an
inert `<FluentToolbar>` element in the DOM.

CI runs `dotnet build` without `-warnaserror` and no project sets `TreatWarningsAsErrors` (measured: the
workflow and every `.csproj` were searched). A migration that missed one of them would pass CI.

A fresh build of `main` in a clean worktree reports **0 warnings** (measured), so making warnings fatal
costs nothing today.

### Removed parameters compile with no diagnostic at all

Read: `FluentComponentBase.AdditionalAttributes` has `CaptureUnmatchedValues = true`, so a parameter that
no longer exists is rendered as an HTML attribute. Not even a warning. Found in our code by the API diff:

| Parameter | Component | Uses | What breaks |
|---|---|---|---|
| `Typo` | `FluentLabel` | 13 | Every heading renders as body text |
| `Color` | `FluentLabel` | 2 | Error text loses its colour |
| `Role`, `Orientation` | `FluentDivider` | 3 | Toolbar separators become horizontal rules |
| `AriaLabel` | `FluentButton` | 3 | Icon-only Start, Stop and Refresh buttons lose their accessible name |
| `SelectedOption`, `SelectedOptionChanged` | `FluentSelect` | 1 | Choosing a .NET channel does nothing |
| `OnSelectedChange` | `FluentTreeView` | 1 | Selecting a folder no longer lists its files |
| `Fill` | `FluentButton` | 1 | The login button is no longer full width |

`SelectedOption*` and `Fill` were measured silent in the spike: they compiled and were only caught by
re-running the audit. **Correction:** `AriaLabel` and `Fill`, and `Panel1MaxSize` on `FluentSplitter`, were
already silent on 4.14.5. None of them was ever a parameter there. The audit's first run on `main` found all
three, so the icon-only buttons have had no accessible name all along. `OnSelectedChange` is silent by the same mechanism (inferred: its handler failed to
compile for an unrelated reason). Upstream issue #5180 asks for an analyzer for exactly this; none exists.

## 4. Defects a green build still carries

The spike compiles cleanly and passes every test. Each item below is still in it.

### 4.1 The login page throws on load — blocking

Read. `Login.razor` calls `DialogsService.ShowError(...)` in `OnInitializedAsync` when the platform is
neither Linux nor macOS. That check runs in the browser: `RuntimeInformation.IsOSPlatform` delegates to
`OperatingSystem.IsOSPlatform`, whose platform name is `"BROWSER"` under `TARGET_BROWSER` (dotnet/runtime,
`release/10.0`). **The condition is always true.**

- v4 (decompiled): `ShowError` ends in `OnShow?.Invoke(...)`, a no-op while no provider has subscribed —
  which is the case on first load, because `MainLayout` renders the providers after the page body.
- v5 (decompiled): `ShowErrorAsync` throws `FluentServiceProviderException<FluentDialogProvider>` when the
  provider is not available. Every first load of `/login` would fault.

Inferred, and worth checking on 4.x today: after a **logout** the layout is already rendered, the
provider has subscribed, and a "platform not supported" dialog should appear. **The maintainer reports it
does not**, so a link in this reasoning is wrong and has not been found: the provider does subscribe to
`OnShow` (decompiled), and logout does not reload the page. The check tested the browser, not the NAS, and
was deleted regardless.

### 4.2 Number fields load a script from unpkg.com — blocking

Read in the v5.0.1 source: `FluentNumberInput.OnAfterRenderAsync(firstRender)` always calls
`ApplyNumberMaskAsync`, which invokes `TextMasked.applyNumberMask`. The bundle's loader then injects
`https://unpkg.com/imask@7.6.1/dist/imask.min.js` unless `window.IMask` already exists.

Our CSP is `script-src 'self' ...`, so the load is refused, the JS promise rejects and the interop call
throws inside `OnAfterRenderAsync` (inferred). That hits the website dialog's three number fields. Even
with the CSP relaxed, an administration UI fetching code from a public CDN at runtime would be a
supply-chain and offline-availability regression.

Two ways out:

1. Use `FluentTextInput` with `TextInputType.Number` for ports and timeouts: no mask, no CDN. Simplest.
2. Vendor `imask` 7.6.1 into `wwwroot` and load it before the FluentUI initializer, so the loader finds
   `window.IMask` (read in `ExternalLibs.ts`). Adds a third-party file and a license entry to
   `LicenseService` and `LicenseServiceTests`.

Also inferred: the mask formats with the culture's group separator, so under fr-FR port `7120` would
display as `7 120`. Another reason for option 1.

The research pass missed this. It listed only `FluentTextInput` with `MaskPattern` and
`FluentSortableList` as CDN users. The IL scan found `FluentNumberInput` as the third.

### 4.3 Error dialogs throw on angle brackets

Read in the v5.0.1 source:

- `ShowErrorAsync`, `ShowInfoAsync` and `ShowConfirmationAsync` wrap the message in
  `MarkupStringSanitized` as HTML.
- Any `<` or `>` that does not form an allow-listed tag fails the sanitizer.
- `MarkupSanitizedOptions.ThrowOnUnsafe` is a static field that defaults to `true`, so the call throws
  `InvalidOperationException`.

Fifteen call sites pass exception or server text into these dialogs, six of them `ex.Message` straight
from a `catch` block. Measured: `System.Text.Json` reports an HTML body as `'<' is an invalid start of a value...`, and
`HttpClientExtensions` deserializes any 2xx body. So a 200 response carrying an HTML page would make the
error handler itself throw, where v4 displayed the message.

Our `.resx` files contain no angle brackets (measured, 358 entries), so localized text alone is safe.

The fix is one helper that HTML-encodes untrusted text before it reaches a message box. Setting
`ThrowOnUnsafe = false` instead would blank the message silently. Toast titles are rendered as text and
are not sanitized (read in `FluentToast.razor`).

### 4.4 Appearance changes the build cannot see

Upstream, not observed:

- `FluentStack` loses its implicit 10 px gap. That affects 26 stacks.
  `config.DefaultValues.For<FluentStack>()` restores it in one place, in both `Program.cs`.
- `FluentIcon` defaults to `currentColor` instead of the accent colour, for 12 icons. Set
  `Color="Color.Primary"` where the accent mattered.

Read:

- The v4 design tokens used by our CSS are defined nowhere in v5: `--neutral-fill-secondary-rest`,
  `--neutral-fill-secondary-hover`, `--accent-fill-rest` and `--type-ramp-base-line-height`. There is no
  alias layer.
- The selected grid row loses its highlight and the toolbar dividers lose their height. The spike's
  Fluent 2 replacements are guesses.
- Element selectors `fluent-toolbar` and `fluent-tab-panel`, and classes `.empty-content-cell` and
  `.fluent-dialog-footer`, no longer match anything.

## 5. Component mapping

What the spike applied, per component we use. **G** marks a guess to verify visually.

| v4 usage | v5 | Notes |
|---|---|---|
| `FluentStack` ×26 | kept | Default gap lost (§4.4) |
| `FluentLabel` ×23 | kept, form label only | Typography moves to `FluentText As=TextTag.*`; heading sizes **G** |
| `FluentButton` ×23 | kept | `Appearance` → `ButtonAppearance`: Accent→Primary, Neutral/Stealth→Default; `AriaLabel` → `Title` (adds a tooltip) |
| `FluentIcon` ×12 | kept | Colour default changed (§4.4) |
| `FluentValidationMessage` ×10 | kept, generic | Ships in 5.0.1 although the migration guide says removed; Blazilla unaffected |
| `FluentDialogHeader/Body/Footer` ×5 | Body only | `TitleTemplate`, `ChildContent`, `ActionTemplate`; dismiss button via `Header.CloseAction.Visible` |
| `FluentProgressRing` ×4 | `FluentSpinner` | Old name is `[Obsolete]` |
| `FluentToolbar` ×3 | removed | Horizontal `FluentStack`; `role="toolbar"` and arrow-key navigation lost |
| `FluentDivider` ×5 | kept | `Role`/`Orientation` → `Vertical` |
| `FluentSpacer` ×3, `FluentCheckbox` ×3, `FluentCard` ×2 | kept | |
| `FluentSelect` ×2 / `FluentOption` | `FluentSelect<TOption,TValue>` | `SelectedOption*` → `@bind-Value` |
| `FluentTextField` | `FluentTextInput` | `TextFieldType` → `TextInputType` |
| `FluentNumberField` | `FluentNumberInput<int>` | **Do not**: §4.2 |
| `FluentTabs/FluentTab` | kept | `<Header>` fragment → `Header` string |
| `FluentSplitter` | `FluentMultiSplitter` + 2 panes | Upstream a11y issues #5503 and #5505 are open on it |
| `FluentTreeView` | kept | `OnSelectedChange` → `OnSelectedChanged`; `TreeViewItem.LoadingTreeViewItems` became a method |
| `FluentDataGrid`, `PropertyColumn`, `TemplateColumn` | kept | Enum renames only: `Align`, `SortDirection`, `GenerateHeaderOption` |
| `FluentOverlay` | kept | `Opacity` 0.4 → 40 (int percent) |
| `FluentMainLayout`, `FluentHeader` | removed | `FluentLayout` + `FluentLayoutItem Area=Header/Content`; accent header bar not reproduced **G** |
| `FluentLayout` in server `App.razor` | moved | Became a plain `div`; the layout now lives in `MainLayout` |
| `FluentDesignTheme Mode=System` | removed | v5 follows the system theme by default (upstream; also read in the bundle) |
| Toast/Dialog/Tooltip/MessageBar/Menu providers | toast, dialog, tooltip kept | MessageBar now needs `Section`; Menu gone; neither is used, both deleted |

`tree-navigation.js` probably still works. v5 still renders `<fluent-tree-item id=... expanded selected>`
(decompiled), but the chain from `.expanded = true` to the lazy load is not verified.

## 6. Services

**Dialogs.** Read, and decompiled by the spike.

- `DialogParameters` → `DialogOptions`.
- `[CascadingParameter] FluentDialog` → `[CascadingParameter] IDialogInstance`.
- `IDialogContentComponent` is gone. The cascading parameter avoids deriving from `FluentDialogInstance`,
  which would collide with `@inherits WorkingStateBase`.
- Data passes through `options.Parameters[nameof(X.Content)]`, and a wrong key throws.
- `ShowDialogAsync` now returns the `DialogResult` when the dialog **closes**. The nested releases dialog
  in `WebSiteConfigurationDialog` therefore keeps its parent open until it closes; v4 did not wait.
- `Modal = true` renders a dialog that ignores overlay clicks, which matches v4's
  `PreventDismissOnOverlayClick`.
- `TrapFocus = false` and `PreventScroll` have no equivalent.

**Toasts.** `IToastService` → `INotificationService.Show*ToastAsync(title, ...)`. A toast now needs a
title, so the spike passes the message as the title.

**Built-in strings.** v5 adds `IFluentLocalizer` (111 keys) but ships English only. That covers the
confirmation buttons, "This field is required" and the grid's empty state. Routing it through our
`ILocalizer` would localize them, which v4 never did. It is an improvement, not a regression, and
optional.

## 7. CSP and third-party code

- `style-src 'unsafe-inline'` stays required. Components still render `style` attributes, and
  `FluentLayout` emits an inline `<style>` element (read).
- No `eval` or `new Function` in the v5 bundle (measured; v4 had one `new Function`). `script-src 'self'`
  suffices for FluentUI itself.
- `default-fuib.css` is fetched at runtime from `_content/`. Our CSP sets no `connect-src`, so
  `default-src 'self'` covers it (read).
- CDN loaders exist for IMask (`FluentNumberInput`, masked `FluentTextInput`) and Sortable
  (`FluentSortableList`). Any future use of those components needs §4.2's choice.
- The research pass claimed v5 no longer auto-imports its scoped CSS bundle. **Measured false for this
  build:** the published `Askyl.Dsm.WebHosting.Ui.styles.css` still `@import`s the v5 bundle.

## 8. Payload

Measured on `dotnet publish -c Release`, Brotli sizes:

| Asset | 4.14.5 | 5.0.1 |
|---|---|---|
| `Components.wasm` | 167 KB | 209 KB |
| `Icons.Regular.wasm` (trimmed) | 6.9 KB | 6.6 KB |
| `lib.module.js` | 75 KB | 89 KB |
| scoped CSS bundle | 11.9 KB | 13.6 KB |
| all `.wasm` together | 3.36 MB | 3.37 MB |

The FluentUI assembly grows, others shrink, and the total moves by about 9 KB of WASM. Not a factor.

## 9. Upstream state

Upstream, as of 2026-10-23:

- **Releases:** 5.0.0 GA on 2026-09-25, 5.0.1 on 2026-10-14. 4.14.5, the last 4.x, shipped on 2026-09-30,
  after 5.0.
- **Support:** the `archive-v4` README says v4 is "no longer supported" and will be maintained "for a
  while" without features. No end date is published, and no 4.x package carries a deprecation or
  vulnerability flag.
- **Icons:** the 5.0.1 Icons package depends on Components ≥ 4.14.5, and every `Icons.Regular` type of
  4.14.5 survives. Icons are not part of the problem.
- **Open issues that touch us:**
  - #5516: `FluentDataGrid` leaks its key listener. Our dialogs create and dispose grids repeatedly.
  - #5468: a regression in 5.0.1 tabs, in a nested case we do not have.
  - #5503 and #5505: `FluentMultiSplitter` accessibility.
  - #5250: debounced inputs can submit stale values.
- **Browser baseline:** tooltips and dropdowns, `FluentSelect` included, rely on CSS anchor positioning.
  That needs Chrome 125, Firefox 147 or Safari 26, or an app-supplied polyfill.
- **Tooling:** a `fluentui-mcp` tool (NuGet `Microsoft.FluentUI.AspNetCore.McpServer`) exposes the
  migration guide per component. There is no analyzer and no compatibility package.
- **Docs drift:** the migration guide disagrees with the 5.0.1 assembly in several places (for example
  `FluentValidationMessage`, the toast replacement, `ActionTemplate`). Trust the assembly.

## 10. Plan

### 10.1 Gates first, on 4.x, in their own pull request

1. `TreatWarningsAsErrors` in `src/Directory.Build.props`. It turns RZ10012 and CS0618 into build failures.
   The baseline is 0 warnings, and AGENTS.md already requires fixing every warning, so the line enforces
   an existing rule. The narrower alternative is `WarningsAsErrors` for `RZ10012;RZ2012;CS0618`.
2. **Prove it by reverting:** add `<FluentToolbar2 />` to a page, watch the build fail, then remove it.
3. Delete the platform check in `Login.razor` (§4.1) once the logout symptom is confirmed or ruled out.

For the silent parameters there is no cheap gate. A test that parses the `.razor` sources and checks
every attribute of every `Fluent*` tag against that component's `[Parameter]` set would close the class.
It is worth it only if the migration goes ahead.

### 10.2 The migration, as one branch

Start from `spike/fluentui-5` or redo it; the mapping in §5 is the work list. On top of the spike:

1. Ports and timeouts on `FluentTextInput TextInputType.Number`, not `FluentNumberInput` (§4.2).
2. An HTML-encoding helper in front of every message box fed by exception or server text (§4.3).
3. `FluentStack` default gap restored, icon colours decided (§4.4).
4. CSS tokens and selectors replaced, checked visually.
5. `docs/ai/technical-architecture.md` updated: its UI tree still names `FluentMainLayout` and
   `FluentDesignTheme`.
6. Version bump, minor: the UI changes everywhere.

### 10.3 What only the maintainer can check

On the NAS, since only the maintainer runs the application:

- **Login:** the page loads with no error dialog, both before and after a logout.
- **Main screen:** header, toolbar spacing and separators, and the selected grid row are visible.
- **Website dialog:**
  - the number fields accept input and show no group separator;
  - Enter submits the form;
  - the releases dialog opens from it.
- **File dialog:**
  - the splitter resizes;
  - selecting a folder lists its files;
  - double-clicking a folder in the grid expands and selects it in the tree.
- **Licenses dialog:** the vertical tabs work.
- **Notifications:** toasts appear, and an error message containing `<` displays instead of throwing.
- **Theme:** dark mode follows the system.
- **Accessibility:** icon-only buttons announce a name.
- **Browsers:** one older browser, to check where the `FluentSelect` dropdown is positioned.

## 11. Decisions for the maintainer

- Whether and when to migrate (§1, §9). There is no deadline.
- `TreatWarningsAsErrors` globally, or only for the three codes (§10.1).
- The number inputs: `FluentTextInput`, or vendored IMask (§4.2).
- Whether to accept the Fluent 2 look: the accent header bar and the toolbar semantics have no direct
  equivalent.
- Whether to localize FluentUI's built-in strings through `IFluentLocalizer` (§6).

## 12. Corrections this analysis makes

- **The tracker's "16 errors" was the first wave of 66.** The real cost was hidden behind declaration
  errors (§3).
- **The spike agent's verdict on the login dialog** ("only matters on a non-Linux/macOS client") was
  wrong: in WebAssembly the client is always "Browser" (§4.1).
- **Two research claims were refuted by measurement**:
  - "no app component uses the CDN" — `FluentNumberInput` does (§4.2);
  - "the scoped CSS bundle is no longer imported" — it still is (§7).

## 13. What executing the plan taught

- **The detectors died where predicted, and only there.** Applied on top of the gates, the spike failed
  exactly three tests:
  - the CDN rule, on `RealTimeNumberField`;
  - the website dialog, on a recorded `TextMasked` interop call;
  - the error dialog, with the sanitizer's `InvalidOperationException` on the `JsonException` text.

  §4.3 went from read to measured.
- **A rendering test can deadlock on the new dialog API.** `InvokeAsync(() => shown = ShowDialogAsync(...))`
  is a `Func<Task>`, so bUnit awaited a dialog that never closes. `--blame-hang-timeout` named the test. A
  statement lambda fixes it, and the test comment says why.
- **Three more silent regressions** turned up while removing the `SPIKE:` notes, all outside any audit
  until then:
  - the header bar's accent background;
  - `FocusOnNavigate`'s `fluent-toolbar` selector;
  - the dead `fluent-dialog-footer` class.

  The audit now rejects FluentUI 4 tokens and removed element names in `.razor`, `.css` and `.js`.
- **Every gate was proven by reverting it.** Each one was made to fail by putting back the defect it
  guards against: an unknown tag, `AriaLabel`, a direct message box call, the unencoded helper, the missing
  stack gap, and the 4.x stylesheet.

# Custom Roslyn Analyzers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce AGENTS.md manual check rules at build time via three custom Roslyn analyzers that emit build-blocking errors.

**Architecture:** Single analyzer project (`src/Askyl.Dsm.WebHosting.Analyzers/`) targeting `netstandard2.0`, distributed via `Directory.Build.props` ProjectReference with `OutputItemType="Analyzer"`. Three independent analyzers, each with optional code fix provider.

**Tech Stack:** .NET 10, Roslyn (Microsoft.CodeAnalysis.CSharp), `Microsoft.CodeAnalysis.Testing` for unit tests, xUnit, Moq.

## Global Constraints

- Analyzer project targets `netstandard2.0` (Roslyn compatibility baseline)
- All diagnostics are **errors** (build-blocking) — `DiagnosticSeverity.Error`
- Diagnostic IDs: `ADWH01001`, `ADWH01002`, `ADWH02001`, `ADWH03001`
- Analyzer project added to solution and `Directory.Build.props` for distribution
- Tests use `Microsoft.CodeAnalysis.Testing` (CSharpAnalyzerTest + CSharpCodeFixTest)
- Generated code (`GeneratedCodeAttribute`) excluded from analysis
- Comments in English, messages in English
- Follow project code standards: primary constructors, `String.` pattern, `[LoggerMessage]` logging (N/A for analyzers), target-typed `new`

---

## File Structure

```
src/Askyl.Dsm.WebHosting.Analyzers/
├── Askyl.Dsm.WebHosting.Analyzers.csproj
├── BlankLineAnalyzer.cs
├── BlankLineCodeFixProvider.cs
├── StringStaticMemberAnalyzer.cs
├── StringStaticMemberCodeFixProvider.cs
├── LoggerDirectCallAnalyzer.cs
└── Resources.Designer.cs / Resources.resx

src/Askyl.Dsm.WebHosting.Tests/Analyzers/
├── BlankLineAnalyzerTests.cs
├── StringStaticMemberAnalyzerTests.cs
└── LoggerDirectCallAnalyzerTests.cs
```

---

### Task 1: Create Analyzer Project and Infrastructure

**Files:**
- Create: `src/Askyl.Dsm.WebHosting.Analyzers/Askyl.Dsm.WebHosting.Analyzers.csproj`
- Create: `src/Askyl.Dsm.WebHosting.Analyzers/Resources.resx`
- Create: `src/Askyl.Dsm.WebHosting.Analyzers/Resources.Designer.cs`
- Create: `src/Askyl.Dsm.WebHosting.Analyzers/Askyl.Dsm.WebHosting.Analyzers.cs` (assembly attributes)
- Modify: `src/Askyl.Dsm.WebHosting.slnx` (add analyzer project)
- Modify: `src/Directory.Build.props` (add analyzer project reference)
- Modify: `src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj` (add testing packages + analyzer project reference)

**Interfaces:**
- Produces: Analyzer project buildable, testable infrastructure in Tests project

- [ ] **Step 1: Create the analyzer project file**

Create `src/Askyl.Dsm.WebHosting.Analyzers/Askyl.Dsm.WebHosting.Analyzers.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <DevelopmentDependency>true</DevelopmentDependency>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.11.0" PrivateAssets="all" />
    <PackageReference Update="NETStandard.Library" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create Resources.resx with diagnostic messages**

Create `src/Askyl.Dsm.WebHosting.Analyzers/Resources.resx` with these entries:

| Name | Value |
|------|-------|
| `ADWH01001_Title` | Missing blank line before control flow statement |
| `ADWH01001_Message` | Add a blank line before '{0}' statement (unless first in scope or preceded by a comment) |
| `ADWH01001_Description` | Control flow statements (if, else, foreach, for, while, switch, try, catch) must be preceded by a blank line unless they are the first statement in their scope or immediately preceded by a comment. |
| `ADWH01002_Title` | Missing blank line after control flow statement |
| `ADWH01002_Message` | Add a blank line after the closing brace of '{0}' statement (unless last in scope) |
| `ADWH01002_Description` | Control flow statements must be followed by a blank line after their closing brace unless they are the last statement in their parent scope. |
| `ADWH02001_Title` | Use 'String.' for static member access |
| `ADWH02001_Message` | Use 'String.{0}' instead of 'string.{0}' for static member access |
| `ADWH02001_Description` | Static members of the String type must use the PascalCase 'String.' prefix (e.g., String.Equals, String.IsNullOrWhiteSpace) rather than the lowercase 'string.' keyword. |
| `ADWH03001_Title` | Do not call ILogger.LogXxx methods directly |
| `ADWH03001_Message` | Do not call 'ILogger.{0}' directly; use a [LoggerMessage] source-generated extension method instead |
| `ADWH03001_Description` | All logging must use [LoggerMessage] source-generated extension methods. Direct calls to ILogger.LogInformation(), ILogger.LogError(), ILogger.LogWarning(), ILogger.LogDebug(), ILogger.LogCritical(), and ILogger.Log() are forbidden. |

- [ ] **Step 3: Create Resources.Designer.cs**

Create `src/Askyl.Dsm.WebHosting.Analyzers/Resources.Designer.cs` — a strongly-typed resource accessor. This will be auto-generated by the build, but we need the initial file:

```csharp
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace Askyl.Dsm.WebHosting.Analyzers;

[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
[DesignerCategory("code")]
[EditorBrowsable(EditorBrowsableState.Advanced)]
internal static class Resources
{
    private static ResourceManager? _resourceManager;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
        get => _resourceManager ??= new ResourceManager(typeof(Resources));
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo? Culture
    {
        get => Thread.CurrentThread.CurrentUICulture;
        set
        {
            if (value is not null)
                Thread.CurrentThread.CurrentUICulture = value;
        }
    }

    internal static string ADWH01001_Title => ResourceManager.GetString(nameof(ADWH01001_Title), Culture)!;
    internal static string ADWH01001_Message => ResourceManager.GetString(nameof(ADWH01001_Message), Culture)!;
    internal static string ADWH01001_Description => ResourceManager.GetString(nameof(ADWH01001_Description), Culture)!;
    internal static string ADWH01002_Title => ResourceManager.GetString(nameof(ADWH01002_Title), Culture)!;
    internal static string ADWH01002_Message => ResourceManager.GetString(nameof(ADWH01002_Message), Culture)!;
    internal static string ADWH01002_Description => ResourceManager.GetString(nameof(ADWH01002_Description), Culture)!;
    internal static string ADWH02001_Title => ResourceManager.GetString(nameof(ADWH02001_Title), Culture)!;
    internal static string ADWH02001_Message => ResourceManager.GetString(nameof(ADWH02001_Message), Culture)!;
    internal static string ADWH02001_Description => ResourceManager.GetString(nameof(ADWH02001_Description), Culture)!;
    internal static string ADWH03001_Title => ResourceManager.GetString(nameof(ADWH03001_Title), Culture)!;
    internal static string ADWH03001_Message => ResourceManager.GetString(nameof(ADWH03001_Message), Culture)!;
    internal static string ADWH03001_Description => ResourceManager.GetString(nameof(ADWH03001_Description), Culture)!;
}
```

- [ ] **Step 4: Create assembly attributes file**

Create `src/Askyl.Dsm.WebHosting.Analyzers/Askyl.Dsm.WebHosting.Analyzers.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Askyl.Dsm.WebHosting.Tests")]
```

- [ ] **Step 5: Add analyzer project to solution**

Modify `src/Askyl.Dsm.WebHosting.slnx`, add before the Tests project line:

```xml
  <Project Path="Askyl.Dsm.WebHosting.Analyzers/Askyl.Dsm.WebHosting.Analyzers.csproj" />
```

- [ ] **Step 6: Add analyzer project reference to Directory.Build.props**

Append to `src/Directory.Build.props` before the closing `</Project>`:

```xml
  <ItemGroup>
    <ProjectReference Include="Askyl.Dsm.WebHosting.Analyzers/Askyl.Dsm.WebHosting.Analyzers.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false"
                      SetConfigurations="Configuration=Debug" />
  </ItemGroup>
```

- [ ] **Step 7: Add testing packages to Tests project**

Modify `src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj`, add to the first `<ItemGroup>`:

```xml
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzer.Testing" Version="1.1.2" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Analyzer.Testing" Version="1.1.2" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.CodeFix.Testing" Version="1.1.2" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit" Version="1.1.2" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="4.11.0" />
```

Also add the analyzer project reference to the ProjectReferences ItemGroup:

```xml
    <ProjectReference Include="..\Askyl.Dsm.WebHosting.Analyzers/Askyl.Dsm.WebHosting.Analyzers.csproj" />
```

- [ ] **Step 8: Build and verify**

Run: `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build succeeds with no errors.

- [ ] **Step 9: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Analyzers/ src/Askyl.Dsm.WebHosting.slnx src/Directory.Build.props src/Askyl.Dsm.WebHosting.Tests/Askyl.Dsm.WebHosting.Tests.csproj
git commit -m "feat: create analyzer project infrastructure with diagnostic resources"
```

---

### Task 2: StringStaticMemberAnalyzer (ADWH02001) + Code Fix

**Files:**
- Create: `src/Askyl.Dsm.WebHosting.Analyzers/StringStaticMemberAnalyzer.cs`
- Create: `src/Askyl.Dsm.WebHosting.Analyzers/StringStaticMemberCodeFixProvider.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Analyzers/StringStaticMemberAnalyzerTests.cs`

**Interfaces:**
- Consumes: `Resources.ADWH02001_*` from Task 1
- Produces: Analyzer that detects `string.Equals`, `string.IsNullOrWhiteSpace`, `string.IsNullOrEmpty`, `string.Empty`, `string.Create`, `string.Intern`, `string.IsInterned`, `string.Compare`, `string.Concat`, `string.Copy`, `string.Format`, `string.Join` — emits ADWH02001. Code fix replaces `string.` with `String.`.

- [ ] **Step 1: Write the failing test**

Create `src/Askyl.Dsm.WebHosting.Tests/Analyzers/StringStaticMemberAnalyzerTests.cs`:

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Askyl.Dsm.WebHosting.Analyzers.StringStaticMemberAnalyzer>;

namespace Askyl.Dsm.WebHosting.Tests.Analyzers;

public class StringStaticMemberAnalyzerTests
{
    [Fact]
    public async Task StringEquals_DetectsDiagnostic()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var r = string.Equals("a", "b");
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH02001")
            .WithLocation(5, 25)
            .WithArguments("Equals");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task StringIsNullOrWhiteSpace_DetectsDiagnostic()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var r = string.IsNullOrWhiteSpace("a");
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH02001")
            .WithLocation(5, 25)
            .WithArguments("IsNullOrWhiteSpace");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task StringEmpty_DetectsDiagnostic()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var r = string.Empty;
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH02001")
            .WithLocation(5, 25)
            .WithArguments("Empty");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task StringVariableDeclaration_NoDiagnostic()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    string s = "hello";
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task StringParameterType_NoDiagnostic()
    {
        var testCode = """
            class C
            {
                void M(string s) { }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task StringReturnType_NoDiagnostic()
    {
        var testCode = """
            class C
            {
                string M() => "hello";
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task StringFieldDeclaration_NoDiagnostic()
    {
        var testCode = """
            class C
            {
                string _field;
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~StringStaticMemberAnalyzerTests" --no-build`
Expected: FAIL — analyzer class doesn't exist yet

- [ ] **Step 3: Implement the analyzer**

Create `src/Askyl.Dsm.WebHosting.Analyzers/StringStaticMemberAnalyzer.cs`:

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Askyl.Dsm.WebHosting.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringStaticMemberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ADWH02001";

    public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Resources.ADWH02001_Title,
            messageFormat: Resources.ADWH02001_Message,
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Resources.ADWH02001_Description)];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MemberAccessExpression);
    }

    static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        if (memberAccess.Expression is not IdentifierNameSyntax identifier)
            return;

        if (identifier.Text != "string")
            return;

        var semanticModel = context.SemanticModel;
        var symbolInfo = semanticModel.GetSymbolInfo(identifier);

        if (symbolInfo.Symbol is not INamedTypeSymbol typeSymbol)
            return;

        if (!SymbolEqualityComparer.Default.Equals(typeSymbol, typeSymbol.Compilation.GetTypeByMetadataName("System.String")))
            return;

        var memberName = memberAccess.Name.Identifier.Text;
        var diagnostic = Diagnostic.Create(SupportedDiagnostics[0], memberAccess.GetLocation(), memberName);
        context.ReportDiagnostic(diagnostic);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~StringStaticMemberAnalyzerTests"`
Expected: PASS (all 7 tests)

- [ ] **Step 5: Write code fix test**

Add to `StringStaticMemberAnalyzerTests.cs`:

```csharp
public class StringStaticMemberCodeFixTests
{
    [Fact]
    public async Task CodeFix_ReplacesStringWithString()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var r = string.Equals("a", "b");
                }
            }
            """;

        var fixedCode = """
            class C
            {
                void M()
                {
                    var r = String.Equals("a", "b");
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH02001")
            .WithLocation(5, 25)
            .WithArguments("Equals");

        await VerifyCS.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
```

Update the top of the test file — add the code fix verify import:

```csharp
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Askyl.Dsm.WebHosting.Analyzers.StringStaticMemberAnalyzer>;
using VerifyCodeFixCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<Askyl.Dsm.WebHosting.Analyzers.StringStaticMemberAnalyzer, Askyl.Dsm.WebHosting.Analyzers.StringStaticMemberCodeFixProvider>;
```

- [ ] **Step 6: Run code fix test to verify it fails**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~StringStaticMemberCodeFixTests"`
Expected: FAIL — code fix provider doesn't exist yet

- [ ] **Step 7: Implement the code fix provider**

Create `src/Askyl.Dsm.WebHosting.Analyzers/StringStaticMemberCodeFixProvider.cs`:

```csharp
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Askyl.Dsm.WebHosting.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Shared = true)]
[Shared]
public class StringStaticMemberCodeFixProvider : CodeFixProvider
{
    public const string Title = "Use 'String.' for static member access";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [StringStaticMemberAnalyzer.DiagnosticId];

    public sealed override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var memberAccess = root?.FindToken(diagnosticSpan.Start).Parent
            ?.AncestorsAndSelf()
            .OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(m => m.Expression is IdentifierNameSyntax id && id.Text == "string");

        if (memberAccess is null)
            return;

        var newIdentifier = SyntaxFactory.Identifier("String")
            .WithLeadingTrivia(memberAccess.Expression.GetLeadingTrivia())
            .WithTrailingTrivia(memberAccess.Expression.GetTrailingTrivia());

        var newNode = memberAccess.WithExpression(SyntaxFactory.IdentifierName(newIdentifier));
        var newRoot = root!.ReplaceNode(memberAccess, newNode);

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                _ => Task.FromResult(new Document(context.Document, newRoot)),
                Title),
            diagnostic);
    }
}
```

- [ ] **Step 8: Run all tests to verify they pass**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~StringStaticMember"`
Expected: PASS (all 8 tests)

- [ ] **Step 9: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Analyzers/StringStaticMember*.cs src/Askyl.Dsm.WebHosting.Tests/Analyzers/StringStaticMemberAnalyzerTests.cs
git commit -m "feat: implement StringStaticMemberAnalyzer (ADWH02001) with code fix"
```

---

### Task 3: LoggerDirectCallAnalyzer (ADWH03001)

**Files:**
- Create: `src/Askyl.Dsm.WebHosting.Analyzers/LoggerDirectCallAnalyzer.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Analyzers/LoggerDirectCallAnalyzerTests.cs`

**Interfaces:**
- Consumes: `Resources.ADWH03001_*` from Task 1
- Produces: Analyzer that detects direct calls to `logger.LogInformation()`, `logger.LogError()`, `logger.LogWarning()`, `logger.LogDebug()`, `logger.LogCritical()`, `logger.Log()` — emits ADWH03001. No code fix.

- [ ] **Step 1: Write the failing tests**

Create `src/Askyl.Dsm.WebHosting.Tests/Analyzers/LoggerDirectCallAnalyzerTests.cs`:

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Askyl.Dsm.WebHosting.Analyzers.LoggerDirectCallAnalyzer>;

namespace Askyl.Dsm.WebHosting.Tests.Analyzers;

public class LoggerDirectCallAnalyzerTests
{
    [Fact]
    public async Task LogInformation_DetectsDiagnostic()
    {
        var testCode = """
            using Microsoft.Extensions.Logging;

            class C
            {
                void M(ILogger logger)
                {
                    logger.LogInformation("test");
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH03001")
            .WithLocation(7, 21)
            .WithArguments("LogInformation");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task LogError_DetectsDiagnostic()
    {
        var testCode = """
            using Microsoft.Extensions.Logging;

            class C
            {
                void M(ILogger logger)
                {
                    logger.LogError(null, "test");
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH03001")
            .WithLocation(7, 21)
            .WithArguments("LogError");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task LogWarning_DetectsDiagnostic()
    {
        var testCode = """
            using Microsoft.Extensions.Logging;

            class C
            {
                void M(ILogger logger)
                {
                    logger.LogWarning("test");
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH03001")
            .WithLocation(7, 21)
            .WithArguments("LogWarning");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task LogDebug_DetectsDiagnostic()
    {
        var testCode = """
            using Microsoft.Extensions.Logging;

            class C
            {
                void M(ILogger logger)
                {
                    logger.LogDebug("test");
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH03001")
            .WithLocation(7, 21)
            .WithArguments("LogDebug");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task LogCritical_DetectsDiagnostic()
    {
        var testCode = """
            using Microsoft.Extensions.Logging;

            class C
            {
                void M(ILogger logger)
                {
                    logger.LogCritical("test");
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH03001")
            .WithLocation(7, 21)
            .WithArguments("LogCritical");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task Log_DetectsDiagnostic()
    {
        var testCode = """
            using Microsoft.Extensions.Logging;

            class C
            {
                void M(ILogger logger)
                {
                    logger.Log(LogLevel.Information, 0, "test", null, null);
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH03001")
            .WithLocation(7, 21)
            .WithArguments("Log");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ExtensionMethodCall_NoDiagnostic()
    {
        var testCode = """
            using Microsoft.Extensions.Logging;

            static class Extensions
            {
                public static void MyExtension(this ILogger logger, string msg) { }
            }

            class C
            {
                void M(ILogger logger)
                {
                    logger.MyExtension("test");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task NonLoggerVariable_NoDiagnostic()
    {
        var testCode = """
            class Logger
            {
                public void LogInformation(string msg) { }
            }

            class C
            {
                void M()
                {
                    var logger = new Logger();
                    logger.LogInformation("test");
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~LoggerDirectCallAnalyzerTests"`
Expected: FAIL — analyzer class doesn't exist yet

- [ ] **Step 3: Implement the analyzer**

Create `src/Askyl.Dsm.WebHosting.Analyzers/LoggerDirectCallAnalyzer.cs`:

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Askyl.Dsm.WebHosting.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LoggerDirectCallAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ADWH03001";

    static readonly ImmutableHashSet<string> _forbiddenMethods =
        ["Log", "LogInformation", "LogDebug", "LogWarning", "LogError", "LogCritical"];

    public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [new DiagnosticDescriptor(
            id: DiagnosticId,
            title: Resources.ADWH03001_Title,
            messageFormat: Resources.ADWH03001_Message,
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Resources.ADWH03001_Description)];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.Text;
        if (!_forbiddenMethods.Contains(methodName))
            return;

        var semanticModel = context.SemanticModel;
        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        var containingType = methodSymbol.ContainingType;
        if (containingType.ToString() != "Microsoft.Extensions.Logging.ILogger")
            return;

        var diagnostic = Diagnostic.Create(SupportedDiagnostics[0], memberAccess.Name.GetLocation(), methodName);
        context.ReportDiagnostic(diagnostic);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~LoggerDirectCallAnalyzerTests"`
Expected: PASS (all 8 tests)

- [ ] **Step 5: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Analyzers/LoggerDirectCallAnalyzer.cs src/Askyl.Dsm.WebHosting.Tests/Analyzers/LoggerDirectCallAnalyzerTests.cs
git commit -m "feat: implement LoggerDirectCallAnalyzer (ADWH03001)"
```

---

### Task 4: BlankLineAnalyzer (ADWH01001/01002) + Code Fix

**Files:**
- Create: `src/Askyl.Dsm.WebHosting.Analyzers/BlankLineAnalyzer.cs`
- Create: `src/Askyl.Dsm.WebHosting.Analyzers/BlankLineCodeFixProvider.cs`
- Create: `src/Askyl.Dsm.WebHosting.Tests/Analyzers/BlankLineAnalyzerTests.cs`

**Interfaces:**
- Consumes: `Resources.ADWH01001_*`, `Resources.ADWH01002_*` from Task 1
- Produces: Analyzer that checks blank lines before/after control flow statements. Two diagnostic IDs: ADWH01001 (missing before), ADWH01002 (missing after). Code fix adds blank lines.

- [ ] **Step 1: Write the failing tests**

Create `src/Askyl.Dsm.WebHosting.Tests/Analyzers/BlankLineAnalyzerTests.cs`:

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Askyl.Dsm.WebHosting.Analyzers.BlankLineAnalyzer>;
using VerifyCodeFixCS = Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<Askyl.Dsm.WebHosting.Analyzers.BlankLineAnalyzer, Askyl.Dsm.WebHosting.Analyzers.BlankLineCodeFixProvider>;

namespace Askyl.Dsm.WebHosting.Tests.Analyzers;

public class BlankLineAnalyzerTests
{
    [Fact]
    public async Task IfStatement_NotFirstInScope_DetectsMissingBlankLineBefore()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var x = 1;
                    if (x > 0)
                    {
                    }
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01001")
            .WithLocation(6, 5)
            .WithArguments("if");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task IfStatement_FirstInScope_NoDiagnostic()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    if (true)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task IfStatement_PrecededByComment_NoDiagnostic()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var x = 1;
                    // check value
                    if (x > 0)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task IfStatement_WithBlankLineBefore_NoDiagnostic()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var x = 1;

                    if (x > 0)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task IfStatement_NotLastInScope_DetectsMissingBlankLineAfter()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    if (true)
                    {
                    }
                    var x = 1;
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01002")
            .WithLocation(5, 5)
            .WithArguments("if");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task IfStatement_LastInScope_NoDiagnostic()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    if (true)
                    {
                    }
                }
            }
            """;

        await VerifyCS.VerifyAnalyzerAsync(testCode);
    }

    [Fact]
    public async Task ElseStatement_DetectsMissingBlankLineBefore()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    if (true)
                    {
                    }
                    else
                    {
                    }
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01001")
            .WithLocation(8, 5)
            .WithArguments("else");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ForeachStatement_DetectsMissingBlankLineBefore()
    {
        var testCode = """
            using System.Collections.Generic;

            class C
            {
                void M(List<int> items)
                {
                    var x = 1;
                    foreach (var item in items)
                    {
                    }
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01001")
            .WithLocation(8, 5)
            .WithArguments("foreach");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task TryStatement_DetectsMissingBlankLineBefore()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var x = 1;
                    try
                    {
                    }
                    catch
                    {
                    }
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01001")
            .WithLocation(6, 5)
            .WithArguments("try");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task CatchClause_DetectsMissingBlankLineBefore()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    try
                    {
                    }
                    catch
                    {
                    }
                    var x = 1;
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01001")
            .WithLocation(7, 5)
            .WithArguments("catch");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task WhileStatement_DetectsMissingBlankLineBefore()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var x = 1;
                    while (x > 0)
                    {
                        x--;
                    }
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01001")
            .WithLocation(6, 5)
            .WithArguments("while");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task ForStatement_DetectsMissingBlankLineBefore()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var x = 1;
                    for (int i = 0; i < x; i++)
                    {
                    }
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01001")
            .WithLocation(6, 5)
            .WithArguments("for");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }

    [Fact]
    public async Task SwitchStatement_DetectsMissingBlankLineBefore()
    {
        var testCode = """
            class C
            {
                void M(int x)
                {
                    var y = 2;
                    switch (x)
                    {
                    }
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01001")
            .WithLocation(6, 5)
            .WithArguments("switch");

        await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
    }
}

public class BlankLineCodeFixTests
{
    [Fact]
    public async Task CodeFix_AddsBlankLineBeforeIf()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    var x = 1;
                    if (x > 0)
                    {
                    }
                }
            }
            """;

        var fixedCode = """
            class C
            {
                void M()
                {
                    var x = 1;

                    if (x > 0)
                    {
                    }
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01001")
            .WithLocation(6, 5)
            .WithArguments("if");

        await VerifyCodeFixCS.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }

    [Fact]
    public async Task CodeFix_AddsBlankLineAfterIf()
    {
        var testCode = """
            class C
            {
                void M()
                {
                    if (true)
                    {
                    }
                    var x = 1;
                }
            }
            """;

        var fixedCode = """
            class C
            {
                void M()
                {
                    if (true)
                    {
                    }

                    var x = 1;
                }
            }
            """;

        var expected = DiagnosticResult.Expect("ADWH01002")
            .WithLocation(5, 5)
            .WithArguments("if");

        await VerifyCodeFixCS.VerifyCodeFixAsync(testCode, expected, fixedCode);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~BlankLineAnalyzerTests"`
Expected: FAIL — analyzer class doesn't exist yet

- [ ] **Step 3: Implement the analyzer**

Create `src/Askyl.Dsm.WebHosting.Analyzers/BlankLineAnalyzer.cs`:

```csharp
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Askyl.Dsm.WebHosting.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class BlankLineAnalyzer : DiagnosticAnalyzer
{
    public const string MissingBeforeId = "ADWH01001";
    public const string MissingAfterId = "ADWH01002";

    static readonly DiagnosticDescriptor _missingBeforeRule = new(
        id: MissingBeforeId,
        title: Resources.ADWH01001_Title,
        messageFormat: Resources.ADWH01001_Message,
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.ADWH01001_Description);

    static readonly DiagnosticDescriptor _missingAfterRule = new(
        id: MissingAfterId,
        title: Resources.ADWH01002_Title,
        messageFormat: Resources.ADWH01002_Message,
        category: "Style",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.ADWH01002_Description);

    public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [_missingBeforeRule, _missingAfterRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode,
            SyntaxKind.IfStatement,
            SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement,
            SyntaxKind.ForStatement,
            SyntaxKind.ForEachStatement,
            SyntaxKind.ForEachVariableStatement,
            SyntaxKind.SwitchStatement,
            SyntaxKind.TryStatement);
    }

    static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var node = context.Node;
        AnalyzeBlankLineBefore(context, node);
        AnalyzeBlankLineAfter(context, node);
    }

    static void AnalyzeBlankLineBefore(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        var parent = node.Parent;
        if (parent is null)
            return;

        var previousSibling = GetPreviousStatement(node);
        if (previousSibling is null)
            return;

        if (HasBlankLineBetween(previousSibling, node))
            return;

        if (IsPrecededByComment(node))
            return;

        var keyword = node switch
        {
            IfStatementSyntax => "if",
            ElseStatementSyntax => "else",
            WhileStatementSyntax => "while",
            DoStatementSyntax => "do",
            ForStatementSyntax => "for",
            ForEachStatementSyntax or ForEachVariableStatementSyntax => "foreach",
            SwitchStatementSyntax => "switch",
            TryStatementSyntax => "try",
            _ => node.ToString().Split('\n')[0]
        };

        var diagnostic = Diagnostic.Create(_missingBeforeRule, node.GetLocation(), keyword);
        context.ReportDiagnostic(diagnostic);
    }

    static void AnalyzeBlankLineAfter(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        var parent = node.Parent;
        if (parent is null)
            return;

        var nextSibling = GetNextStatement(node);
        if (nextSibling is null)
            return;

        if (HasBlankLineBetween(node, nextSibling))
            return;

        var keyword = node switch
        {
            IfStatementSyntax => "if",
            WhileStatementSyntax => "while",
            DoStatementSyntax => "do",
            ForStatementSyntax => "for",
            ForEachStatementSyntax or ForEachVariableStatementSyntax => "foreach",
            SwitchStatementSyntax => "switch",
            TryStatementSyntax => "try",
            _ => node.ToString().Split('\n')[0]
        };

        var diagnostic = Diagnostic.Create(_missingAfterRule, node.GetLocation(), keyword);
        context.ReportDiagnostic(diagnostic);
    }

    static SyntaxNode? GetPreviousStatement(SyntaxNode node)
    {
        var parent = node.Parent;
        if (parent is null)
            return null;

        if (parent is Block block)
        {
            var index = block.Statements.IndexOf(node);
            return index > 0 ? block.Statements[index - 1] : null;
        }

        if (parent is IfStatementSyntax ifStmt && ifStmt.Else is not null && ifStmt.Else.Statement == node)
        {
            return ifStmt.Statement;
        }

        if (parent is TryStatementSyntax tryStmt)
        {
            var catches = tryStmt.Handlers;
            for (var i = 0; i < catches.Count; i++)
            {
                if (catches[i] == node)
                    return i > 0 ? catches[i - 1] : tryStmt.Block;
            }

            if (tryStmt.Finally is not null && tryStmt.Finally.Statement == node)
                return tryStmt.Handlers.Count > 0 ? tryStmt.Handlers[^1] : tryStmt.Block;
        }

        return null;
    }

    static SyntaxNode? GetNextStatement(SyntaxNode node)
    {
        var parent = node.Parent;
        if (parent is null)
            return null;

        if (parent is Block block)
        {
            var index = block.Statements.IndexOf(node);
            return index < block.Statements.Count - 1 ? block.Statements[index + 1] : null;
        }

        return null;
    }

    static bool HasBlankLineBetween(SyntaxNode first, SyntaxNode second)
    {
        var firstEndLine = first.GetLocation().GetLineSpan().EndLinePosition.Line;
        var secondStartLine = second.GetLocation().GetLineSpan().StartLinePosition.Line;
        return secondStartLine - firstEndLine > 1;
    }

    static bool IsPrecededByComment(SyntaxNode node)
    {
        var leadingTrivia = node.GetLeadingTrivia();
        foreach (var trivia in leadingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                return true;

            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                return false;
        }

        return false;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~BlankLineAnalyzerTests"`
Expected: PASS (all 13 tests)

- [ ] **Step 5: Run code fix tests to verify they fail**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~BlankLineCodeFixTests"`
Expected: FAIL — code fix provider doesn't exist yet

- [ ] **Step 6: Implement the code fix provider**

Create `src/Askyl.Dsm.WebHosting.Analyzers/BlankLineCodeFixProvider.cs`:

```csharp
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Askyl.Dsm.WebHosting.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Shared = true)]
[Shared]
public class BlankLineCodeFixProvider : CodeFixProvider
{
    public const string AddBeforeTitle = "Add blank line before statement";
    public const string AddAfterTitle = "Add blank line after statement";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [BlankLineAnalyzer.MissingBeforeId, BlankLineAnalyzer.MissingAfterId];

    public sealed override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var node = root?.FindNode(diagnostic.Location.SourceSpan);

        if (node is null)
            return;

        if (diagnostic.Id == BlankLineAnalyzer.MissingBeforeId)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    AddBeforeTitle,
                    _ => AddBlankLineBeforeAsync(context.Document, node, context.CancellationToken),
                    AddBeforeTitle),
                diagnostic);
        }
        else if (diagnostic.Id == BlankLineAnalyzer.MissingAfterId)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    AddAfterTitle,
                    _ => AddBlankLineAfterAsync(context.Document, node, context.CancellationToken),
                    AddAfterTitle),
                diagnostic);
        }
    }

    static async Task<Document> AddBlankLineBeforeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var trivia = SyntaxFactory.ElasticEmptyLine;
        var newNode = node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, trivia));
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root?.ReplaceNode(node, newNode);
        return document.WithSyntaxRoot(newRoot);
    }

    static async Task<Document> AddBlankLineAfterAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var trivia = SyntaxFactory.ElasticEmptyLine;
        var newNode = node.WithTrailingTrivia(node.GetTrailingTrivia().Add(trivia));
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root?.ReplaceNode(node, newNode);
        return document.WithSyntaxRoot(newRoot);
    }
}
```

- [ ] **Step 7: Run all tests to verify they pass**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests --filter "FullyQualifiedName~BlankLine"`
Expected: PASS (all 15 tests)

- [ ] **Step 8: Commit**

```bash
git add src/Askyl.Dsm.WebHosting.Analyzers/BlankLine*.cs src/Askyl.Dsm.WebHosting.Tests/Analyzers/BlankLineAnalyzerTests.cs
git commit -m "feat: implement BlankLineAnalyzer (ADWH01001/01002) with code fix"
```

---

### Task 5: Verify Full Solution Build and Integration

**Files:**
- Modify: none (verification only)

**Interfaces:**
- Consumes: All analyzers from Tasks 2-4, project infrastructure from Task 1
- Produces: Verified that all analyzers fire correctly against the existing codebase

- [ ] **Step 1: Run full solution build**

Run: `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`
Expected: Build produces diagnostics from the analyzers (existing code may trigger violations — this is expected and confirms the analyzers work).

- [ ] **Step 2: Run full test suite**

Run: `dotnet test ./src/Askyl.Dsm.WebHosting.Tests`
Expected: All existing tests pass, plus all new analyzer tests pass.

- [ ] **Step 3: Verify analyzer diagnostics are emitted as errors**

Inspect build output — confirm diagnostics appear with `error ADWHxxxxx` prefix (not warnings).

- [ ] **Step 4: Commit**

```bash
git commit -m "ci: verify analyzer integration — build and tests pass"
```
(If no changes, skip this step and note that verification passed.)

---

### Task 6: Add .editorconfig Suppression Rules and Final Polish

**Files:**
- Modify: `.editorconfig` (if exists at solution level) — add documentation comments for analyzer IDs

**Interfaces:**
- Consumes: All analyzers from Tasks 2-4
- Produces: Documented suppression rules for edge cases

- [ ] **Step 1: Check for existing .editorconfig**

Check if `.editorconfig` exists at the solution root or `src/` level. If it exists, verify analyzer IDs are documented.

- [ ] **Step 2: Add suppression guidance to AGENTS.md**

Update `AGENTS.md` Section 4 ("Build & Format Workflow") to mention that the three manual checks (blank lines, String/String pattern, logger direct calls) are now enforced by analyzers ADWH01001, ADWH01002, ADWH02001, ADWH03001. Move these from "Manual Checks Required" to "Tooling-Enforced Patterns".

- [ ] **Step 3: Run format**

Run: `dotnet format ./src/Askyl.Dsm.WebHosting.slnx --verbosity quiet`

- [ ] **Step 4: Run final build**

Run: `dotnet build /nr:false ./src/Askyl.Dsm.WebHosting.slnx`

- [ ] **Step 5: Commit**

```bash
git add AGENTS.md .editorconfig
git commit -m "docs: update AGENTS.md — manual checks now enforced by analyzers"
```

---

## Self-Review

### 1. Spec Coverage

| Spec Requirement | Task | Status |
|-----------------|------|--------|
| BlankLineAnalyzer (ADWH01001/01002) | Task 4 | ✅ |
| BlankLineCodeFix | Task 4 | ✅ |
| StringStaticMemberAnalyzer (ADWH02001) | Task 2 | ✅ |
| StringStaticMemberCodeFix | Task 2 | ✅ |
| LoggerDirectCallAnalyzer (ADWH03001) | Task 3 | ✅ |
| No auto-fix for LoggerDirectCall | Task 3 | ✅ (no code fix provider) |
| All diagnostics are errors | Tasks 2-4 | ✅ (DiagnosticSeverity.Error) |
| Project structure matches spec | Task 1 | ✅ |
| Directory.Build.props distribution | Task 1 | ✅ |
| Unit tests with Microsoft.CodeAnalysis.Testing | Tasks 2-4 | ✅ |
| Generated code exclusion | Tasks 2-4 | ✅ (ConfigureGeneratedCodeAnalysis) |
| Test projects suppressible via .editorconfig | Task 6 | ✅ |

### 2. Placeholder Scan

- No "TBD", "TODO", "implement later" found
- No "add appropriate error handling" — analyzers are self-contained
- No "write tests" without code — all test code is inline
- No "similar to Task N" — each task is self-contained
- All types, methods, and references are defined within tasks

### 3. Type Consistency

- `StringStaticMemberAnalyzer.DiagnosticId` = `"ADWH02001"` — used consistently in tests and code fix
- `BlankLineAnalyzer.MissingBeforeId` = `"ADWH01001"`, `MissingAfterId` = `"ADWH01002"` — used consistently
- `LoggerDirectCallAnalyzer.DiagnosticId` = `"ADWH03001"` — used consistently
- `Resources.ADWH01001_Title`, etc. — names match .resx entries
- `CSharpAnalyzerTest<T>` and `CSharpCodeFixTest<T, F>` — generic parameters match analyzer/fix provider types

All consistent. No issues found.

---

## Follow-Up Tasks (Post-Implementation)

### Task 7: Forbid Blank Lines Before `else` / `catch`

**Rationale:** Removing `else`/`catch` from the "require blank line before" rule exposed existing blank lines that visually disconnect `if-else` and `try-catch` pairs. These should be forbidden.

**New rules:**
- `ADWH01003` — extra blank line before `else` (code fix: remove)
- `ADWH01004` — extra blank line before `catch` (code fix: remove)

**Scope:** BlankLineAnalyzer extension — detect blank line between `if` body closing `}` and `else` keyword, and between `if` body / prior `catch` and `catch` keyword.

**Impact:** Existing codebase has ~20+ violations (one per `if-else` and `try-catch` across all projects). Code fix is trivial (remove blank line).

using Askyl.Dsm.WebHosting.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Askyl.Dsm.WebHosting.Tests.Analyzers;

public class BlankLineAnalyzerTests
{
    [Fact]
    public async Task IfStatement_NotFirstInScope_DetectsMissingBlankLineBefore()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            TestCode = """
                class C
                {
                    void M()
                    {
                        var x = 1;
                        [|if|] (x > 0)
                        {
                        }
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task IfStatement_FirstInScope_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task IfStatement_PrecededByComment_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task IfStatement_WithBlankLineBefore_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task IfStatement_NotLastInScope_DetectsMissingBlankLineAfter()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                """,
            ExpectedDiagnostics = {
                new DiagnosticResult(BlankLineAnalyzer.MissingAfterId, DiagnosticSeverity.Error)
                    .WithSpan(5, 9, 5, 11)
                    .WithArguments("if"),
            },
        }.RunAsync();
    }

    [Fact]
    public async Task IfStatement_LastInScope_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    void M()
                    {
                        if (true)
                        {
                        }
                    }
                }
                """,
        }.RunAsync();
    }



    [Fact]
    public async Task ForeachStatement_DetectsMissingBlankLineBefore()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M(List<int> items)
                    {
                        var x = 1;
                        [|foreach|] (var item in items)
                        {
                        }
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task TryStatement_DetectsMissingBlankLineBefore()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                """,
            ExpectedDiagnostics = {
                new DiagnosticResult(BlankLineAnalyzer.MissingBeforeId, DiagnosticSeverity.Error)
                    .WithSpan(6, 9, 6, 12)
                    .WithArguments("try"),
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TryCatch_NoDiagnosticBeforeCatch()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task WhileStatement_DetectsMissingBlankLineBefore()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            TestCode = """
                class C
                {
                    void M()
                    {
                        var x = 1;
                        [|while|] (x > 0)
                        {
                            x--;
                        }
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task ForStatement_DetectsMissingBlankLineBefore()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            TestCode = """
                class C
                {
                    void M()
                    {
                        var x = 1;
                        [|for|] (int i = 0; i < x; i++)
                        {
                        }
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task SwitchStatement_DetectsMissingBlankLineBefore()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            TestCode = """
                class C
                {
                    void M(int x)
                    {
                        var y = 2;
                        [|switch|] (x)
                        {
                        }
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task DoWhileStatement_DetectsMissingBlankLineBefore()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            TestCode = """
                class C
                {
                    void M()
                    {
                        var x = 1;
                        [|do|]
                        {
                            x--;
                        } while (x > 0);
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task IfElse_ExtraBlankLineBeforeElse_DetectsDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                """,
            ExpectedDiagnostics = {
                new DiagnosticResult(BlankLineAnalyzer.ExtraBeforeElseId, DiagnosticSeverity.Error)
                    .WithSpan(9, 9, 9, 13),
            },
        }.RunAsync();
    }

    [Fact]
    public async Task IfElse_NoBlankLineBeforeElse_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task TryCatch_ExtraBlankLineBeforeCatch_DetectsDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                    }
                }
                """,
            ExpectedDiagnostics = {
                new DiagnosticResult(BlankLineAnalyzer.ExtraBeforeCatchId, DiagnosticSeverity.Error)
                    .WithSpan(9, 9, 9, 14),
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TryCatch_NoBlankLineBeforeCatch_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task TryCatchMultiple_ExtraBlankLineBeforeSecondCatch_DetectsDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    void M()
                    {
                        try
                        {
                        }
                        catch (System.Exception)
                        {
                        }

                        catch
                        {
                        }
                    }
                }
                """,
            ExpectedDiagnostics = {
                new DiagnosticResult(BlankLineAnalyzer.ExtraBeforeCatchId, DiagnosticSeverity.Error)
                    .WithSpan(12, 9, 12, 14),
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TryCatchFinally_ExtraBlankLineBeforeFinally_DetectsDiagnostic()
    {
        await new CSharpAnalyzerTest<BlankLineAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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

                        finally
                        {
                        }
                    }
                }
                """,
            ExpectedDiagnostics = {
                new DiagnosticResult(BlankLineAnalyzer.ExtraBeforeCatchId, DiagnosticSeverity.Error)
                    .WithSpan(12, 9, 12, 16),
            },
        }.RunAsync();
    }
}

public class BlankLineCodeFixTests
{
    [Fact]
    public async Task CodeFix_AddsBlankLineBeforeIf()
    {
        await new CSharpCodeFixTest<BlankLineAnalyzer, BlankLineCodeFixProvider, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            TestCode = """
                class C
                {
                    void M()
                    {
                        var x = 1;
                        [|if|] (x > 0)
                        {
                        }
                    }
                }
                """,
            FixedCode = """
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
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task CodeFix_AddsBlankLineAfterIf()
    {
        await new CSharpCodeFixTest<BlankLineAnalyzer, BlankLineCodeFixProvider, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                """,
            FixedCode = """
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
                """,
            ExpectedDiagnostics = {
                new DiagnosticResult(BlankLineAnalyzer.MissingAfterId, DiagnosticSeverity.Error)
                    .WithSpan(5, 9, 5, 11)
                    .WithArguments("if"),
            },
        }.RunAsync();
    }

    [Fact]
    public async Task CodeFix_RemovesBlankLineBeforeElse()
    {
        await new CSharpCodeFixTest<BlankLineAnalyzer, BlankLineCodeFixProvider, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                """,
            FixedCode = """
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
                """,
            ExpectedDiagnostics = {
                new DiagnosticResult(BlankLineAnalyzer.ExtraBeforeElseId, DiagnosticSeverity.Error)
                    .WithSpan(9, 9, 9, 13),
            },
        }.RunAsync();
    }

    [Fact]
    public async Task CodeFix_RemovesBlankLineBeforeCatch()
    {
        await new CSharpCodeFixTest<BlankLineAnalyzer, BlankLineCodeFixProvider, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
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
                    }
                }
                """,
            FixedCode = """
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
                    }
                }
                """,
            ExpectedDiagnostics = {
                new DiagnosticResult(BlankLineAnalyzer.ExtraBeforeCatchId, DiagnosticSeverity.Error)
                    .WithSpan(9, 9, 9, 14),
            },
        }.RunAsync();
    }
}

using Askyl.Dsm.WebHosting.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Askyl.Dsm.WebHosting.Tests.Analyzers;

public class StringStaticMemberAnalyzerTests
{
    [Fact]
    public async Task StringEquals_DetectsDiagnostic()
    {
        await new CSharpAnalyzerTest<StringStaticMemberAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    void M()
                    {
                        var r = string.[|Equals|]("a", "b");
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task StringIsNullOrWhiteSpace_DetectsDiagnostic()
    {
        await new CSharpAnalyzerTest<StringStaticMemberAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    void M()
                    {
                        var r = string.[|IsNullOrWhiteSpace|]("a");
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task StringEmpty_DetectsDiagnostic()
    {
        await new CSharpAnalyzerTest<StringStaticMemberAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    void M()
                    {
                        var r = string.[|Empty|];
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task StringVariableDeclaration_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<StringStaticMemberAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    void M()
                    {
                        string s = "hello";
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task StringParameterType_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<StringStaticMemberAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    void M(string s) { }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task StringReturnType_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<StringStaticMemberAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    string M() => "hello";
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task StringFieldDeclaration_NoDiagnostic()
    {
        await new CSharpAnalyzerTest<StringStaticMemberAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestCode = """
                class C
                {
                    string _field;
                }
                """,
        }.RunAsync();
    }
}

public class StringStaticMemberCodeFixTests
{
    [Fact]
    public async Task CodeFix_ReplacesStringWithString()
    {
        await new CSharpCodeFixTest<StringStaticMemberAnalyzer, StringStaticMemberCodeFixProvider, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
      TestCode = """
                 using System;

                 class C
                 {
                     void M()
                     {
                         var r = string.[|Equals|]("a", "b");
                     }
                 }
                 """,
            FixedCode = """
                 using System;

                 class C
                 {
                     void M()
                     {
                         var r = String.Equals("a", "b");
                     }
                 }
                 """,
        }.RunAsync();
    }
}

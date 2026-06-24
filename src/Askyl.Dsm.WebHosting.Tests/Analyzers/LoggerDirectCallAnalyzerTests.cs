using Askyl.Dsm.WebHosting.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Askyl.Dsm.WebHosting.Tests.Analyzers;

public class LoggerDirectCallAnalyzerTests
{
    const string ILoggerStub = """
        using System;

        namespace Microsoft.Extensions.Logging
        {
            public struct EventId
            {
                public EventId(int id) { }
                public int Id => 0;
                public static implicit operator EventId(int id) => new(id);
            }

            public enum LogLevel { Trace, Debug, Information, Warning, Error, Critical }

            public interface ILogger
            {
                void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);
                void LogInformation(string message);
                void LogDebug(string message);
                void LogWarning(string message);
                void LogError(Exception? exception, string message);
                void LogCritical(string message);
            }
        }
        """;

    [Fact]
    public async Task LogInformation_DetectsDiagnostic()
    {
        await new LoggerDirectCallAnalyzerTest
        {
            TestCode = $$"""
                {{ILoggerStub}}

                class C
                {
                    void M(Microsoft.Extensions.Logging.ILogger logger)
                    {
                        logger.[|LogInformation|]("test");
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task LogError_DetectsDiagnostic()
    {
        await new LoggerDirectCallAnalyzerTest
        {
            TestCode = $$"""
                {{ILoggerStub}}

                class C
                {
                    void M(Microsoft.Extensions.Logging.ILogger logger)
                    {
                        logger.[|LogError|](null, "test");
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task LogWarning_DetectsDiagnostic()
    {
        await new LoggerDirectCallAnalyzerTest
        {
            TestCode = $$"""
                {{ILoggerStub}}

                class C
                {
                    void M(Microsoft.Extensions.Logging.ILogger logger)
                    {
                        logger.[|LogWarning|]("test");
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task LogDebug_DetectsDiagnostic()
    {
        await new LoggerDirectCallAnalyzerTest
        {
            TestCode = $$"""
                {{ILoggerStub}}

                class C
                {
                    void M(Microsoft.Extensions.Logging.ILogger logger)
                    {
                        logger.[|LogDebug|]("test");
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task LogCritical_DetectsDiagnostic()
    {
        await new LoggerDirectCallAnalyzerTest
        {
            TestCode = $$"""
                {{ILoggerStub}}

                class C
                {
                    void M(Microsoft.Extensions.Logging.ILogger logger)
                    {
                        logger.[|LogCritical|]("test");
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task Log_DetectsDiagnostic()
    {
        await new LoggerDirectCallAnalyzerTest
        {
            TestCode = $$"""
                {{ILoggerStub}}

                class C
                {
                    void M(Microsoft.Extensions.Logging.ILogger logger)
                    {
                        logger.[|Log|](Microsoft.Extensions.Logging.LogLevel.Information, 0, "test", null, null);
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task ExtensionMethodCall_NoDiagnostic()
    {
        await new LoggerDirectCallAnalyzerTest
        {
            TestCode = $$"""
                {{ILoggerStub}}

                static class Extensions
                {
                    public static void MyExtension(this Microsoft.Extensions.Logging.ILogger logger, string msg) { }
                }

                class C
                {
                    void M(Microsoft.Extensions.Logging.ILogger logger)
                    {
                        logger.MyExtension("test");
                    }
                }
                """,
        }.RunAsync();
    }

    [Fact]
    public async Task NonLoggerVariable_NoDiagnostic()
    {
        await new LoggerDirectCallAnalyzerTest
        {
            TestCode = """
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
                """,
        }.RunAsync();
    }
}

public class LoggerDirectCallAnalyzerTest
    : CSharpAnalyzerTest<LoggerDirectCallAnalyzer, DefaultVerifier>
{
    public LoggerDirectCallAnalyzerTest()
    {
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
    }
}

using System.Collections.Immutable;

namespace Askyl.Dsm.WebHosting.Analyzers;

static class AnalyzerConstants
{
    public const string DiagnosticCategoryUsage = "Usage";
    public const string DiagnosticCategoryStyle = "Style";

    public const string StringKeyword = "string";
    public const string StringTypeName = "System.String";
    public const string ILoggerFullName = "Microsoft.Extensions.Logging.ILogger";

    public static readonly ImmutableHashSet<string> ForbiddenLoggerMethods =
        ["Log", "LogInformation", "LogDebug", "LogWarning", "LogError", "LogCritical"];

    public const string KeywordIf = "if";
    public const string KeywordElse = "else";
    public const string KeywordWhile = "while";
    public const string KeywordDo = "do";
    public const string KeywordFor = "for";
    public const string KeywordForeach = "foreach";
    public const string KeywordSwitch = "switch";
    public const string KeywordTry = "try";
    public const string KeywordCatch = "catch";
}

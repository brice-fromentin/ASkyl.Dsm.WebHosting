using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Askyl.Dsm.WebHosting.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LoggerDirectCallAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ADWH03001";

    static readonly DiagnosticDescriptor s_rule = new(
        id: DiagnosticId,
        title: Resources.ADWH03001_Title,
        messageFormat: Resources.ADWH03001_Message,
        category: AnalyzerConstants.DiagnosticCategoryUsage,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.ADWH03001_Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [s_rule];

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

        if (!AnalyzerConstants.ForbiddenLoggerMethods.Contains(methodName))
            return;

        var semanticModel = context.SemanticModel;
        var symbolInfo = semanticModel.GetSymbolInfo(memberAccess);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        var containingType = methodSymbol.ContainingType;

        if (containingType.ToString() != AnalyzerConstants.ILoggerFullName)
            return;

        var diagnostic = Diagnostic.Create(s_rule, memberAccess.Name.GetLocation(), methodName);
        context.ReportDiagnostic(diagnostic);
    }
}

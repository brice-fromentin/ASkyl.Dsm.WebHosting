using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Askyl.Dsm.WebHosting.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringStaticMemberAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ADWH02001";

    static readonly DiagnosticDescriptor s_rule = new(
        id: DiagnosticId,
        title: Resources.ADWH02001_Title,
        messageFormat: Resources.ADWH02001_Message,
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.ADWH02001_Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [s_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleMemberAccessExpression);
    }

    static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        var expression = memberAccess.Expression;

        if (expression is IdentifierNameSyntax identifier)
        {
            if (identifier.Identifier.Text != "string")
                return;
        }
        else if (expression is PredefinedTypeSyntax predefined)
        {
            if (predefined.Keyword.Text != "string")
                return;
        }
        else
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(expression);

        if (symbolInfo.Symbol is not INamedTypeSymbol typeSymbol)
            return;

        if (!SymbolEqualityComparer.Default.Equals(typeSymbol, context.SemanticModel.Compilation.GetTypeByMetadataName("System.String")))
            return;

        var memberName = memberAccess.Name.Identifier.Text;
        context.ReportDiagnostic(Diagnostic.Create(s_rule, memberAccess.Name.GetLocation(), memberName));
    }
}

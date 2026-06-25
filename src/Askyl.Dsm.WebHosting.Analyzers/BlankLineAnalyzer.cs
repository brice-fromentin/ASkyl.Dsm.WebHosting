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
    public const string ExtraBeforeElseId = "ADWH01003";
    public const string ExtraBeforeCatchId = "ADWH01004";

    static readonly DiagnosticDescriptor _missingBeforeRule = new(
        id: MissingBeforeId,
        title: Resources.ADWH01001_Title,
        messageFormat: Resources.ADWH01001_Message,
        category: AnalyzerConstants.DiagnosticCategoryStyle,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.ADWH01001_Description);

    static readonly DiagnosticDescriptor _missingAfterRule = new(
        id: MissingAfterId,
        title: Resources.ADWH01002_Title,
        messageFormat: Resources.ADWH01002_Message,
        category: AnalyzerConstants.DiagnosticCategoryStyle,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.ADWH01002_Description);

    static readonly DiagnosticDescriptor _extraBeforeElseRule = new(
        id: ExtraBeforeElseId,
        title: Resources.ADWH01003_Title,
        messageFormat: Resources.ADWH01003_Message,
        category: AnalyzerConstants.DiagnosticCategoryStyle,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.ADWH01003_Description);

    static readonly DiagnosticDescriptor _extraBeforeCatchRule = new(
        id: ExtraBeforeCatchId,
        title: Resources.ADWH01004_Title,
        messageFormat: Resources.ADWH01004_Message,
        category: AnalyzerConstants.DiagnosticCategoryStyle,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Resources.ADWH01004_Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [_missingBeforeRule, _missingAfterRule, _extraBeforeElseRule, _extraBeforeCatchRule];

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

        switch (node)
        {
            case IfStatementSyntax ifStmt:
                AnalyzeControlFlowStatement(context, ifStmt);
                AnalyzeElseBlankLine(context, ifStmt);
                break;

            case TryStatementSyntax tryStmt:
                AnalyzeControlFlowStatement(context, tryStmt);
                AnalyzeCatchBlankLines(context, tryStmt);
                break;

            default:
                AnalyzeControlFlowStatement(context, node);
                break;
        }
    }

    static void AnalyzeControlFlowStatement(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        AnalyzeBlankLineBefore(context, node);
        AnalyzeBlankLineAfter(context, node);
    }

    static void AnalyzeBlankLineBefore(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        var parent = node.Parent;

        if (parent is null)
        {
            return;
        }

        var previousSibling = GetPreviousStatement(node);

        if (previousSibling is null)
        {
            return;
        }

        if (HasBlankLineBetween(previousSibling, node))
        {
            return;
        }

        if (IsPrecededByComment(node))
        {
            return;
        }

        var keyword = GetKeywordName(node);
        var location = GetKeywordLocation(node);

        var diagnostic = Diagnostic.Create(_missingBeforeRule, location, keyword);
        context.ReportDiagnostic(diagnostic);
    }

    static void AnalyzeBlankLineAfter(SyntaxNodeAnalysisContext context, SyntaxNode node)
    {
        var parent = node.Parent;

        if (parent is null)
        {
            return;
        }

        var nextSibling = GetNextStatement(node);

        if (nextSibling is null)
        {
            return;
        }

        if (HasBlankLineBetween(node, nextSibling))
        {
            return;
        }

        var keyword = GetKeywordName(node);
        var location = GetKeywordLocation(node);

        var diagnostic = Diagnostic.Create(_missingAfterRule, location, keyword);
        context.ReportDiagnostic(diagnostic);
    }

    static void AnalyzeElseBlankLine(SyntaxNodeAnalysisContext context, IfStatementSyntax ifStmt)
    {
        var elseClause = ifStmt.Else;

        if (elseClause is null)
        {
            return;
        }

        var elseKeyword = elseClause.ElseKeyword;

        if (!HasBlankLineBetween(ifStmt.Statement, elseKeyword))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(_extraBeforeElseRule, elseKeyword.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }

    static void AnalyzeCatchBlankLines(SyntaxNodeAnalysisContext context, TryStatementSyntax tryStmt)
    {
        var catches = tryStmt.Catches;

        if (catches.Count == 0)
        {
            return;
        }

        // Check blank line between try block and first catch
        if (HasBlankLineBetween(tryStmt.Block, catches[0].CatchKeyword))
        {
            var diagnostic = Diagnostic.Create(_extraBeforeCatchRule, catches[0].CatchKeyword.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        // Check blank line between consecutive catch blocks
        for (var i = 1; i < catches.Count; i++)
        {
            var previousCatch = catches[i - 1];
            var currentCatch = catches[i];

            if (HasBlankLineBetween(previousCatch.Block, currentCatch.CatchKeyword))
            {
                var diagnostic = Diagnostic.Create(_extraBeforeCatchRule, currentCatch.CatchKeyword.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        // Check blank line between last catch and finally
        if (tryStmt.Finally is not null && catches.Count > 0)
        {
            var lastCatch = catches[catches.Count - 1];

            if (HasBlankLineBetween(lastCatch.Block, tryStmt.Finally.FinallyKeyword))
            {
                var diagnostic = Diagnostic.Create(_extraBeforeCatchRule, tryStmt.Finally.FinallyKeyword.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
        else if (tryStmt.Finally is not null)
        {
            if (HasBlankLineBetween(tryStmt.Block, tryStmt.Finally.FinallyKeyword))
            {
                var diagnostic = Diagnostic.Create(_extraBeforeCatchRule, tryStmt.Finally.FinallyKeyword.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    static string GetKeywordName(SyntaxNode node) => node switch
    {
        IfStatementSyntax => AnalyzerConstants.KeywordIf,
        WhileStatementSyntax => AnalyzerConstants.KeywordWhile,
        DoStatementSyntax => AnalyzerConstants.KeywordDo,
        ForStatementSyntax => AnalyzerConstants.KeywordFor,
        ForEachStatementSyntax or ForEachVariableStatementSyntax => AnalyzerConstants.KeywordForeach,
        SwitchStatementSyntax => AnalyzerConstants.KeywordSwitch,
        TryStatementSyntax => AnalyzerConstants.KeywordTry,
        _ => node.ToString().Split('\n')[0]
    };

    static Location GetKeywordLocation(SyntaxNode node) => node switch
    {
        IfStatementSyntax ifStmt => ifStmt.IfKeyword.GetLocation(),
        WhileStatementSyntax whileStmt => whileStmt.WhileKeyword.GetLocation(),
        DoStatementSyntax doStmt => doStmt.DoKeyword.GetLocation(),
        ForStatementSyntax forStmt => forStmt.ForKeyword.GetLocation(),
        ForEachStatementSyntax forEachStmt => forEachStmt.ForEachKeyword.GetLocation(),
        ForEachVariableStatementSyntax forEachVarStmt => forEachVarStmt.ForEachKeyword.GetLocation(),
        SwitchStatementSyntax switchStmt => switchStmt.SwitchKeyword.GetLocation(),
        TryStatementSyntax tryStmt => tryStmt.TryKeyword.GetLocation(),
        _ => node.GetLocation()
    };

    static SyntaxNode? GetPreviousStatement(SyntaxNode node)
    {
        var parent = node.Parent;

        if (parent is null)
        {
            return null;
        }

        if (parent is BlockSyntax block)
        {
            var index = block.Statements.IndexOf((StatementSyntax)node);
            return index > 0 ? block.Statements[index - 1] : null;
        }

        if (parent is TryStatementSyntax tryStmt)
        {
            var catches = tryStmt.Catches;

            for (var i = 0; i < catches.Count; i++)
            {
                if (catches[i] == node)
                {
                    return i > 0 ? catches[i - 1] : tryStmt.Block;
                }
            }

            if (tryStmt.Finally is not null && tryStmt.Finally.Block == node)
            {
                return tryStmt.Catches.Count > 0 ? tryStmt.Catches[tryStmt.Catches.Count - 1] : tryStmt.Block;
            }
        }

        return null;
    }

    static SyntaxNode? GetNextStatement(SyntaxNode node)
    {
        var parent = node.Parent;

        if (parent is null)
        {
            return null;
        }

        if (parent is BlockSyntax block)
        {
            var index = block.Statements.IndexOf((StatementSyntax)node);
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

    static bool HasBlankLineBetween(SyntaxNode first, SyntaxToken second)
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
            {
                return true;
            }

            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return false;
            }
        }

        return false;
    }

    static bool IsPrecededByComment(SyntaxToken token)
    {
        var leadingTrivia = token.LeadingTrivia;

        foreach (var trivia in leadingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                return true;
            }

            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                return false;
            }
        }

        return false;
    }
}

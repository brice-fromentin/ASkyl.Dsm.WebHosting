using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Askyl.Dsm.WebHosting.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
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
            .FirstOrDefault(m => IsStringExpression(m.Expression));

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
                _ => Task.FromResult(context.Document.WithSyntaxRoot(newRoot)),
                Title),
            diagnostic);
    }

    static bool IsStringExpression(ExpressionSyntax expression)
    {
        if (expression is IdentifierNameSyntax id && id.Identifier.Text == "string")
            return true;

        if (expression is PredefinedTypeSyntax predefined && predefined.Keyword.Text == "string")
            return true;

        return false;
    }
}

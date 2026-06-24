using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Askyl.Dsm.WebHosting.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp)]
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
        var newLine = SyntaxFactory.EndOfLine("\n");
        var existingTrivia = node.GetLeadingTrivia();
        var newTrivia = existingTrivia.Insert(0, newLine);
        var newNode = node.WithLeadingTrivia(newTrivia);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root?.ReplaceNode(node, newNode);
        return document.WithSyntaxRoot(newRoot);
    }

    static async Task<Document> AddBlankLineAfterAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var newLine = SyntaxFactory.EndOfLine("\n");
        var newTrivia = node.GetTrailingTrivia().Add(newLine);
        var newNode = node.WithTrailingTrivia(newTrivia);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root?.ReplaceNode(node, newNode);
        return document.WithSyntaxRoot(newRoot);
    }
}

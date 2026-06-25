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
    public const string RemoveBeforeElseTitle = "Remove blank line before 'else'";
    public const string RemoveBeforeCatchTitle = "Remove blank line before 'catch'";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        [BlankLineAnalyzer.MissingBeforeId, BlankLineAnalyzer.MissingAfterId, BlankLineAnalyzer.ExtraBeforeElseId, BlankLineAnalyzer.ExtraBeforeCatchId];

    public sealed override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var node = root?.FindNode(diagnostic.Location.SourceSpan);

        if (node is null)
        {
            return;
        }

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
        else if (diagnostic.Id == BlankLineAnalyzer.ExtraBeforeElseId)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    RemoveBeforeElseTitle,
                    _ => RemoveBlankLineBeforeAsync(context.Document, node),
                    RemoveBeforeElseTitle),
                diagnostic);
        }
        else if (diagnostic.Id == BlankLineAnalyzer.ExtraBeforeCatchId)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    RemoveBeforeCatchTitle,
                    _ => RemoveBlankLineBeforeAsync(context.Document, node),
                    RemoveBeforeCatchTitle),
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

        if (newRoot is null)
        {
            return document;
        }

        return document.WithSyntaxRoot(newRoot);
    }

    static async Task<Document> AddBlankLineAfterAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var newLine = SyntaxFactory.EndOfLine("\n");
        var newTrivia = node.GetTrailingTrivia().Add(newLine);
        var newNode = node.WithTrailingTrivia(newTrivia);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root?.ReplaceNode(node, newNode);

        if (newRoot is null)
        {
            return document;
        }

        return document.WithSyntaxRoot(newRoot);
    }

    static async Task<Document> RemoveBlankLineBeforeAsync(Document document, SyntaxNode node)
    {
        var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        // Find the previous sibling statement
        var parent = node.Parent;

        if (parent is BlockSyntax block)
        {
            var index = block.Statements.IndexOf((StatementSyntax)node);

            if (index > 0)
            {
                var previousStatement = block.Statements[index - 1];
                var previousTrailing = previousStatement.GetTrailingTrivia();
                var newTrailing = RemoveTrailingBlankLine(previousTrailing);

                if (newTrailing != previousTrailing)
                {
                    var newPrevious = previousStatement.WithTrailingTrivia(newTrailing);
                    var newRoot = root.ReplaceNode(previousStatement, newPrevious);
                    return document.WithSyntaxRoot(newRoot);
                }
            }
        }
        else if (parent is IfStatementSyntax ifStmt && node is ElseClauseSyntax elseClause)
        {
            // For else, the previous node is the if statement body
            var ifBody = ifStmt.Statement;
            var ifBodyTrailing = ifBody.GetTrailingTrivia();
            var newIfBodyTrailing = RemoveTrailingBlankLine(ifBodyTrailing);

            if (newIfBodyTrailing != ifBodyTrailing)
            {
                var newIfBody = ifBody.WithTrailingTrivia(newIfBodyTrailing);
                var newIfStmt = ifStmt.WithStatement(newIfBody);
                var newRoot = root.ReplaceNode(ifStmt, newIfStmt);
                return document.WithSyntaxRoot(newRoot);
            }
        }
        else if (parent is TryStatementSyntax tryStmt)
        {
            // For catch/finally, find the previous catch/finally or try body
            SyntaxNode? previousNode = null;

            if (node is CatchClauseSyntax catchClause)
            {
                var index = tryStmt.Catches.IndexOf(catchClause);

                if (index > 0)
                {
                    previousNode = tryStmt.Catches[index - 1];
                }
                else
                {
                    previousNode = tryStmt.Block;
                }
            }
            else if (node is FinallyClauseSyntax finallyClause)
            {
                previousNode = tryStmt.Catches.Count > 0 ? tryStmt.Catches[tryStmt.Catches.Count - 1] : tryStmt.Block;
            }

            if (previousNode is not null)
            {
                var previousTrailing = previousNode.GetTrailingTrivia();
                var newTrailing = RemoveTrailingBlankLine(previousTrailing);

                if (newTrailing != previousTrailing)
                {
                    var newPrevious = previousNode.WithTrailingTrivia(newTrailing);
                    var newRoot = root.ReplaceNode(previousNode, newPrevious);
                    return document.WithSyntaxRoot(newRoot);
                }
            }
        }

        return document;
    }

    static SyntaxTriviaList RemoveTrailingBlankLine(SyntaxTriviaList trivia)
    {
        // Find the first consecutive EndOfLine at the end and remove all but one
        var i = trivia.Count - 1;

        while (i >= 0 && trivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
        {
            i--;
        }

        // If we found more than one EndOfLine at the end, keep only one
        if (i < trivia.Count - 2)
        {
            return SyntaxFactory.TriviaList(trivia.Take(i + 1).Append(SyntaxFactory.EndOfLine("\n")));
        }

        return trivia;
    }
}

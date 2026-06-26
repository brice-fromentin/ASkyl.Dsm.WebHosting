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
            if (root?.FindToken(diagnostic.Location.SourceSpan.Start) is not SyntaxToken elseToken)
            {
                return;
            }

            if (elseToken.Parent is not ElseClauseSyntax elseClause)
            {
                return;
            }

            if (elseClause.Parent is not IfStatementSyntax ifStmt)
            {
                return;
            }

            if (ifStmt.Statement is not BlockSyntax ifBody)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    RemoveBeforeElseTitle,
                    _ => RemoveBlankLineBetweenNodesAsync(context.Document, ifBody.CloseBraceToken, elseToken),
                    RemoveBeforeElseTitle),
                diagnostic);
        }
        else if (diagnostic.Id == BlankLineAnalyzer.ExtraBeforeCatchId)
        {
            if (root?.FindToken(diagnostic.Location.SourceSpan.Start) is not SyntaxToken keywordToken)
            {
                return;
            }

            var parentNode = keywordToken.Parent;

            if (parentNode is CatchClauseSyntax catchClause && catchClause.Parent is TryStatementSyntax tryStmt)
            {
                var index = tryStmt.Catches.IndexOf(catchClause);
                BlockSyntax prevBlock = index > 0 ? ((CatchClauseSyntax)tryStmt.Catches[index - 1]).Block : tryStmt.Block;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        RemoveBeforeCatchTitle,
                        _ => RemoveBlankLineBetweenNodesAsync(context.Document, prevBlock.CloseBraceToken, keywordToken),
                        RemoveBeforeCatchTitle),
                    diagnostic);
            }
            else if (parentNode is FinallyClauseSyntax finallyClause && finallyClause.Parent is TryStatementSyntax tryStmt2)
            {
                BlockSyntax prevBlock2 = tryStmt2.Catches.Count > 0
                    ? ((CatchClauseSyntax)tryStmt2.Catches[tryStmt2.Catches.Count - 1]).Block
                    : tryStmt2.Block;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        RemoveBeforeCatchTitle,
                        _ => RemoveBlankLineBetweenNodesAsync(context.Document, prevBlock2.CloseBraceToken, keywordToken),
                        RemoveBeforeCatchTitle),
                    diagnostic);
            }
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

    static async Task<Document> RemoveBlankLineBetweenNodesAsync(Document document, SyntaxToken closingBrace, SyntaxToken keyword)
    {
        var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        // The blank line between } and else/catch/finally is typically represented as:
        // - }'s trailing trivia: EndOfLine (newline after })
        // - keyword's leading trivia: EndOfLine (blank line) + Whitespace (indentation)
        // To remove the blank line, strip leading EndOfLines from the keyword.
        var newKeyword = RemoveLeadingBlankLinesToken(keyword);

        if (newKeyword != keyword)
        {
            var newRoot = root.ReplaceToken(keyword, newKeyword);
            return document.WithSyntaxRoot(newRoot);
        }

        // Fallback: try removing extra EndOfLines from closing brace's trailing trivia
        var newClosingBrace = RemoveExtraTrailingEndOfLinesToken(closingBrace);

        if (newClosingBrace != closingBrace)
        {
            var newRoot = root.ReplaceToken(closingBrace, newClosingBrace);
            return document.WithSyntaxRoot(newRoot);
        }

        return document;
    }

    static SyntaxToken RemoveLeadingBlankLinesToken(SyntaxToken token)
    {
        // Remove leading EndOfLine(s) from the token's leading trivia
        // These represent the blank line(s) before the keyword
        var leading = token.LeadingTrivia;

        if (leading.Count == 0)
        {
            return token;
        }

        // Check if first trivia is EndOfLine (blank line)
        if (leading[0].IsKind(SyntaxKind.EndOfLineTrivia))
        {
            // Keep only non-EndOfLine trivia (preserve indentation whitespace)
            var newLeading = SyntaxFactory.TriviaList(leading.Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia)));

            if (newLeading != leading)
            {
                return token.WithLeadingTrivia(newLeading);
            }
        }

        return token;
    }

    static SyntaxToken RemoveExtraTrailingEndOfLinesToken(SyntaxToken token)
    {
        var trailing = token.TrailingTrivia;
        var newTrailing = RemoveOneEndOfLine(trailing);

        if (newTrailing != trailing)
        {
            return token.WithTrailingTrivia(newTrailing);
        }

        return token;
    }

    static SyntaxTriviaList RemoveOneEndOfLine(SyntaxTriviaList trivia)
    {
        // Count total EndOfLines
        var eolCount = 0;

        for (var i = 0; i < trivia.Count; i++)
        {
            if (trivia[i].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                eolCount++;
            }
        }

        // If more than one EndOfLine, remove the first one
        if (eolCount > 1)
        {
            var result = new List<SyntaxTrivia>();
            var removed = false;

            for (var i = 0; i < trivia.Count; i++)
            {
                if (trivia[i].IsKind(SyntaxKind.EndOfLineTrivia) && !removed)
                {
                    removed = true;
                    continue;
                }

                result.Add(trivia[i]);
            }

            return SyntaxFactory.TriviaList(result);
        }

        return trivia;
    }
}

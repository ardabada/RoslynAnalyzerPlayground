using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ardalyzer.Utilities;

namespace Ardalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamespaceShouldStartWithArdabadaPlaygroundFixer)), Shared]
    public class LogErrorShouldNotBeUsedFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(Descriptors.ARDA003_LogErrorShouldNotBeUsed.Id);

        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxNode = root.FindNode(context.Span);
            string title = string.Format("Use {0}", Constants.LogErrorWithCodeMethodName);
            context.RegisterCodeFix(
               CodeAction.Create(
                   title: title,
                   createChangedDocument: ct => UseLogErrorWithCode(context.Document, root, syntaxNode, ct),
                   equivalenceKey: title),
               context.Diagnostics);
        }

        private async Task<Document> UseLogErrorWithCode(Document document, SyntaxNode root, SyntaxNode invocationNode, CancellationToken cancellationToken)
        {
            var invocationExpression = invocationNode.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var memberAccessExpression = invocationExpression.Expression as MemberAccessExpressionSyntax;
            var newExpression = memberAccessExpression.WithName(SyntaxFactory.IdentifierName(Constants.LogErrorWithCodeMethodName));
            SeparatedSyntaxList<ArgumentSyntax> args = new SeparatedSyntaxList<ArgumentSyntax>();
            args = args.Add(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(Constants.DefaultLogCode))));
            args = args.AddRange(invocationExpression.ArgumentList.Arguments);
            var newInvocation = invocationExpression.WithExpression(newExpression).WithArgumentList(SyntaxFactory.ArgumentList(args));

            var newRoot = root.ReplaceNode(invocationExpression, newInvocation);

            var newDocument = document.WithSyntaxRoot(newRoot);

            var tree = await newDocument.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var compilationUnit = tree.GetRoot() as CompilationUnitSyntax;
            bool hasUsing = compilationUnit.Usings.Any(x => x.Name.ToString() == Constants.LogWithCodeExtensionsNamespace);
            if (!hasUsing)
            {
                compilationUnit = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(Constants.LogWithCodeExtensionsNamespace).NormalizeWhitespace()).WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
                newRoot = compilationUnit.SyntaxTree.GetRoot();
                newDocument = document.WithSyntaxRoot(newRoot);
            }

            return newDocument;
        }
    }
}

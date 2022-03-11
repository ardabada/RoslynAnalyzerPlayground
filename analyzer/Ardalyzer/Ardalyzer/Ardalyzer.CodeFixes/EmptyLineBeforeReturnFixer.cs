using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Ardalyzer.Utilities;

namespace Ardalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyLineBeforeReturnFixer)), Shared]
    public class EmptyLineBeforeReturnFixer : CodeFixProvider
	{
		private const string Title = "Place one empty line before return";

		public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(Descriptors.ARDA011_EmptyLineBeforeReturn.Id);

        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var returnStatementSyntax = root.FindNode(context.Span) as ReturnStatementSyntax;
			if (returnStatementSyntax is null) return;

			context.RegisterCodeFix(
				CodeAction.Create(
					title: Title,
					createChangedDocument: ct => AddEmptyLineBeforeReturn(context.Document, returnStatementSyntax, ct),
					equivalenceKey: Title),
				context.Diagnostics);
		}

		private async Task<Document> AddEmptyLineBeforeReturn(Document document, ReturnStatementSyntax returnStatementSyntax, CancellationToken cancellationToken)
		{
			var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
			var newLiteral = SyntaxFactory.ReturnStatement(returnStatementSyntax.Expression)
				.WithLeadingTrivia(SyntaxFactory.EndOfLine(string.Empty))
				.WithTrailingTrivia(returnStatementSyntax.GetTrailingTrivia());

			editor.ReplaceNode(returnStatementSyntax, newLiteral);

			var nodesInsideBlock = returnStatementSyntax.Parent.ChildNodes().ToList();
			var indexInsideBlock = nodesInsideBlock.IndexOf(returnStatementSyntax);
			if (indexInsideBlock > 0)
			{
				var previousNode = nodesInsideBlock[indexInsideBlock - 1];
				bool previousNodeHasEndOfLine = previousNode.HasTrailingTrivia && previousNode.GetTrailingTrivia().Count(x => x.Kind() == SyntaxKind.EndOfLineTrivia) == 1;
				if (!previousNodeHasEndOfLine)
				{
					var newPreviousNode = previousNode.WithTrailingTrivia(SyntaxFactory.EndOfLine(string.Empty));
					editor.ReplaceNode(previousNode, newPreviousNode);
				}
			}
			return editor.GetChangedDocument();
		}
	}
}

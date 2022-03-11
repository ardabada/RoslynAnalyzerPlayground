using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ardalyzer.Utilities;

namespace Ardalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConstantsShouldBeInPascalCaseFixer)), Shared]
    public class ConstantsShouldBeInPascalCaseFixer : CodeFixProvider
    {
        private const string TitleTemplate = "Rename constant to {0}";

        public override ImmutableArray<string> FixableDiagnosticIds => 
            ImmutableArray.Create(Descriptors.ARDA002_ConstantsShouldBeInPascalCase.Id);

        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxNode = root.FindNode(context.Span);
            var variableDeclaratorSyntax = syntaxNode.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
            string newVariableName = variableDeclaratorSyntax.Identifier.ValueText.ToPascalCase();
            string title = string.Format(TitleTemplate, newVariableName);
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: ct => RenameToPascalCase(context.Document, variableDeclaratorSyntax, root, newVariableName),
                    equivalenceKey: title),
                context.Diagnostics);
        }

        private Task<Document> RenameToPascalCase(Document document, VariableDeclaratorSyntax variableDeclaratorSyntax, SyntaxNode root, string newVariableName)
        {
            var newLiteral = SyntaxFactory.VariableDeclarator(
                SyntaxFactory.Identifier(newVariableName),
                variableDeclaratorSyntax.ArgumentList,
                variableDeclaratorSyntax.Initializer)
                .WithLeadingTrivia(variableDeclaratorSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(variableDeclaratorSyntax.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(variableDeclaratorSyntax, newLiteral);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return Task.FromResult(newDocument);
        }
    }
}

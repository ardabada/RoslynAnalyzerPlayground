﻿using System.Collections.Immutable;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NamespaceShouldStartWithArdabadaPlaygroundFixer)), Shared]
    public class NamespaceShouldStartWithArdabadaPlaygroundFixer : CodeFixProvider
    {
        private const string TitleTemplate = "Rename namespace to {0}";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(Descriptors.ARDA001_NamespaceShouldStartWithArdabadaPlayground.Id);

        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxNode = root.FindNode(context.Span);
            var namespaceDeclarationSyntax = syntaxNode.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
            var replacement = string.Format("{0}.{1}", Constants.NamespacePrefix, namespaceDeclarationSyntax.Name.ToString());
            string title = string.Format(TitleTemplate, replacement);
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: ct => RenameNamespace(context.Document, namespaceDeclarationSyntax, root, replacement),
                    equivalenceKey: title),
                context.Diagnostics);
        }

        private Task<Document> RenameNamespace(Document document, NamespaceDeclarationSyntax namespaceDeclarationSyntax, SyntaxNode root, string newNamespaceName)
        {
            var newLiteral = SyntaxFactory.ParseName(newNamespaceName)
                .WithLeadingTrivia(namespaceDeclarationSyntax.Name.GetLeadingTrivia())
                .WithTrailingTrivia(namespaceDeclarationSyntax.Name.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(namespaceDeclarationSyntax.Name, newLiteral);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return Task.FromResult(newDocument);
        }

    }
}

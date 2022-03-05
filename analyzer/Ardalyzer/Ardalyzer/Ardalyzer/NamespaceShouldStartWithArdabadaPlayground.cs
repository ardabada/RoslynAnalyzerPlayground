using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Ardalyzer.Utilities;
using System;

namespace Ardalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamespaceShouldStartWithArdabadaPlayground : DiagnosticAnalyzer
    {
        private const string ExpectedNamespace = "Ardabada.Playground";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.ARDA001_NamespaceShouldStartWithArdabadaPlayground);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(syntaxNodeContext =>
            {
                var namespaceDeclarationSyntax = syntaxNodeContext.Node as NamespaceDeclarationSyntax;
                if (namespaceDeclarationSyntax is null) return;

                string namespaceName = namespaceDeclarationSyntax.Name.ToString();
                bool startWithValidPrefix = namespaceName.StartsWith(ExpectedNamespace + ".", StringComparison.Ordinal) || namespaceName.Equals(ExpectedNamespace, StringComparison.Ordinal);
                if (!startWithValidPrefix)
                {
                    syntaxNodeContext.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.ARDA001_NamespaceShouldStartWithArdabadaPlayground, 
                            namespaceDeclarationSyntax.Name.GetLocation(), 
                            namespaceDeclarationSyntax.Name.ToString()));
                }
            }, SyntaxKind.NamespaceDeclaration);
        }
    }
}

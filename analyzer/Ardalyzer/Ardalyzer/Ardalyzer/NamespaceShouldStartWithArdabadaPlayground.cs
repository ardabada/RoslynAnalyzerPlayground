using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Ardalyzer.Utilities;

namespace Ardalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NamespaceShouldStartWithArdabadaPlayground : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.ARDA001_NamespaceShouldStartWithArdabadaPlayground);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(syntaxNodeContext =>
            {
                if (syntaxNodeContext.Node is not NamespaceDeclarationSyntax namespaceDeclarationSyntax) return;

                string namespaceName = namespaceDeclarationSyntax.Name.ToString();
                bool startWithValidPrefix = namespaceName.StartsWith(Constants.NamespacePrefix + ".", StringComparison.Ordinal) || namespaceName.Equals(Constants.NamespacePrefix, StringComparison.Ordinal);
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

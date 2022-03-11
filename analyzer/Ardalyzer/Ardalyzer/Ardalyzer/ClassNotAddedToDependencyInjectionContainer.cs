using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Ardalyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ardalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassNotAddedToDependencyInjectionContainer : DiagnosticAnalyzer
    {
        private readonly SyntaxKind[] classKeywordsToSkip = new SyntaxKind[] { SyntaxKind.StaticKeyword, SyntaxKind.AbstractKeyword };
        private readonly SyntaxKind[] classMethodModifiersToCount = new SyntaxKind[] { SyntaxKind.PublicKeyword, SyntaxKind.InternalKeyword };
        private readonly SyntaxKind[] classMethodModifiersNotToCount = new SyntaxKind[] { SyntaxKind.PrivateKeyword, SyntaxKind.StaticKeyword, SyntaxKind.ProtectedKeyword };
        private readonly string[] classMethodNamesNotToCount = new string[] { nameof(object.ToString), nameof(object.Equals), nameof(object.GetHashCode) };
        private readonly string[] classNamesToSkip = new string[] { "Program", "Startup" };
        private readonly string[] diRegistrationInvocationMethodNames = new string[] { 
            "AddScoped", "AddSingleton", "AddTransient", 
            "TryAddScoped", "TryAddSingleton", "TryAddTransient" 
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.ARDA012_ClassNotAddedToDependencyInjectionContainer);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var serviceCollectionExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(Constants.MicrosoftServiceCollectionServiceExtensionsType);
                var serviceCollectionDescriptorExtensionsType = compilationContext.Compilation.GetTypeByMetadataName(Constants.MicrosoftServiceCollectionDescriptorExtensionsType);
                if (serviceCollectionExtensionsType is null && serviceCollectionDescriptorExtensionsType is null)
                    return;

                compilationContext.RegisterSyntaxNodeAction(syntaxNodeContext =>
                {
                    var classDeclarationSyntax = syntaxNodeContext.Node as ClassDeclarationSyntax;
                    if (classDeclarationSyntax is null) 
                        return;

                    if (classDeclarationSyntax.Modifiers.Any(x => classKeywordsToSkip.Contains(x.Kind())))
                        return;

                    string className = classDeclarationSyntax.Identifier.ValueText;
                    if (classNamesToSkip.Contains(className))
                        return;

                    var classDeclarationSymbol = syntaxNodeContext.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                    bool isModel = !IsServiceClass(classDeclarationSymbol);
                    if (isModel) return;

                    foreach (var tree in syntaxNodeContext.SemanticModel.Compilation.SyntaxTrees)
                    {
                        var syntaxRoot = tree.GetRoot();
                        var semanticModel = syntaxNodeContext.SemanticModel.Compilation.GetSemanticModel(tree);
                        var invocationExpressions = syntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>();
                        foreach (var invocationExpression in invocationExpressions)
                        {
                            var invokedSymbol = semanticModel.GetSymbolInfo(invocationExpression).Symbol;
                            if (!invokedSymbol.ContainingType.Equals(serviceCollectionExtensionsType, SymbolEqualityComparer.Default) &&
                                !invokedSymbol.ContainingType.Equals(serviceCollectionDescriptorExtensionsType, SymbolEqualityComparer.Default))
                                continue;

                            bool isClassRegistration = IsClassRegistrationInvocation(invokedSymbol as IMethodSymbol, classDeclarationSymbol);
                            if (isClassRegistration)
                            {
                                return;
                            }
                        }
                    }

                    syntaxNodeContext.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.ARDA012_ClassNotAddedToDependencyInjectionContainer, 
                            classDeclarationSyntax.Identifier.GetLocation(), 
                            className));

                }, SyntaxKind.ClassDeclaration);
            });
        }

        private bool IsServiceClass(INamedTypeSymbol symbolInfo)
        {
            var declaringReferences = symbolInfo.DeclaringSyntaxReferences;
            bool isModel = true;
            foreach (var declarationReference in declaringReferences)
            {
                var syntax = declarationReference.GetSyntax() as ClassDeclarationSyntax;
                if (syntax is null) continue;

                var methodDeclarations = syntax.Members.OfType<MethodDeclarationSyntax>();
                bool isValidMethod = false;
                foreach (var methodDeclaration in methodDeclarations)
                {
                    bool isValueMethodName = !classMethodNamesNotToCount.Contains(methodDeclaration.Identifier.ValueText);
                    if (!isValueMethodName) continue;

                    var modifiers = methodDeclaration.Modifiers;
                    bool isValidModifier = true;
                    foreach (var modifier in modifiers)
                    {
                        isValidModifier = classMethodModifiersToCount.Contains(modifier.Kind()) && !classMethodModifiersNotToCount.Contains(modifier.Kind());
                        if (!isValidModifier) break;
                    }

                    if (isValidModifier)
                    {
                        isValidMethod = true;
                        break;
                    }
                }

                if (isValidMethod)
                {
                    isModel = false;
                    break;
                }
            }

            return !isModel;
        }

        private bool IsClassRegistrationInvocation(IMethodSymbol invokedSymbol, INamedTypeSymbol classDeclarationSymbol)
        {
            if (!diRegistrationInvocationMethodNames.Contains(invokedSymbol.Name))
                return false;

            var normalizedInvocationParametersCount = 0;
            if (invokedSymbol.Parameters.Any() && GetFullyQualifiedName(invokedSymbol.Parameters[0]).Equals(Constants.IServiceCollectionType))
            {
                normalizedInvocationParametersCount = invokedSymbol.Parameters.Length - 1;
            }
            else
            {
                normalizedInvocationParametersCount = invokedSymbol.Parameters.Length;
            }

            if (normalizedInvocationParametersCount == 0)
            {
                ITypeSymbol typeArgument = null;
                if (invokedSymbol.TypeArguments.Length == 1) typeArgument = invokedSymbol.TypeArguments[0];
                if (invokedSymbol.TypeArguments.Length == 2) typeArgument = invokedSymbol.TypeArguments[1];

                if (typeArgument == null) return false;

                return GetFullyQualifiedName(typeArgument).Equals(GetFullyQualifiedName(classDeclarationSymbol));
            }

            return false;
        }

        private string GetFullyQualifiedName(ISymbol symbol)
        {
            return symbol.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
        }
    }
}

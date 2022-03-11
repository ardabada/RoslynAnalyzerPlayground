using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ardalyzer.Utilities;

namespace Ardalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LogMessageAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(
				Descriptors.ARDA004_UseStructuredLoggingInsteadOfStringInterpolation,
				Descriptors.ARDA005_UseStructuredLoggingToLogParameterValue,
				Descriptors.ARDA006_UseStructuredLoggingToLogVariableValue,
				Descriptors.ARDA007_UseStructuredLoggingToLogPropertyValue,
				Descriptors.ARDA008_UseStructuredLoggingToLogReturnValue,
				Descriptors.ARDA009_LogMessageShouldBeConstant);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(compilationContext =>
			{
				var loggerType = compilationContext.Compilation.GetTypeByMetadataName(Constants.UtilsLoggerExtensionsType);
				if (loggerType is null)
					return;

				compilationContext.RegisterOperationAction(operationContext =>
				{
                    if (operationContext.Operation is not IInvocationOperation invocationOperation) return;

					var methodSymbol = invocationOperation.TargetMethod;
					if (methodSymbol.MethodKind != MethodKind.Ordinary)
						return;

					if (!methodSymbol.ContainingType.Equals(loggerType, SymbolEqualityComparer.Default))
						return;

					if (!methodSymbol.Name.Equals(Constants.LogErrorWithCodeMethodName))
						return;

					DescriptorWithArguments diagnosticsToReport = AnalyzeInvocation(invocationOperation, operationContext.Compilation);
					if (diagnosticsToReport is not null)
					{
						operationContext.ReportDiagnostic(
							Diagnostic.Create(
								diagnosticsToReport.Descriptor,
								invocationOperation.Syntax.GetLocation(),
								diagnosticsToReport.Arguments));
					}
				}, OperationKind.Invocation);
			});
		}

		private DescriptorWithArguments AnalyzeInvocation(IInvocationOperation invocationOperation, Compilation compilation)
        {
			if (invocationOperation.Syntax is not InvocationExpressionSyntax invocationExpressionSyntax)
				return null;

			var ex = invocationExpressionSyntax.ArgumentList.Arguments[1].Expression;
			return AnalyzeSyntaxNode(ex, compilation);
        }

		private DescriptorWithArguments AnalyzeSyntaxNode(SyntaxNode syntaxNode, Compilation compilation)
        {
			if (syntaxNode is null) return null;

			DescriptorWithArguments diagnostic = null;

			if (syntaxNode is BinaryExpressionSyntax binaryExpressionSyntax)
            {
				if (binaryExpressionSyntax.OperatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PlusToken))
				{
					diagnostic = AnalyzeSyntaxNode(binaryExpressionSyntax.Left, compilation);
					if (diagnostic is not null) return diagnostic;

					diagnostic = AnalyzeSyntaxNode(binaryExpressionSyntax.Right, compilation);
					if (diagnostic is not null) return diagnostic;
				}
				else
                {
					return new DescriptorWithArguments(Descriptors.ARDA009_LogMessageShouldBeConstant);
				}
			}

			if (syntaxNode is ConditionalExpressionSyntax conditionalExpressionSyntax)
			{
				return new DescriptorWithArguments(Descriptors.ARDA009_LogMessageShouldBeConstant);
			}

			if (syntaxNode is IdentifierNameSyntax ||
                syntaxNode is MemberAccessExpressionSyntax s)
            {
				var semanticModel = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
				var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
				var symbol = symbolInfo.Symbol;
				if (symbol is null) return null;

				var references = symbol.DeclaringSyntaxReferences;
				foreach (var reference in references)
				{
					diagnostic = AnalyzeSyntaxNode(reference.GetSyntax(), compilation);
					if (diagnostic is not null) return diagnostic;
                }
			}

			if (syntaxNode is InterpolatedStringExpressionSyntax)
            {
				return new DescriptorWithArguments(Descriptors.ARDA004_UseStructuredLoggingInsteadOfStringInterpolation);
			}

			if (syntaxNode is ParameterSyntax parameterSyntax) 
			{
				return new DescriptorWithArguments(Descriptors.ARDA005_UseStructuredLoggingToLogParameterValue, parameterSyntax.Identifier.ValueText);
			}

			if (syntaxNode is VariableDeclaratorSyntax variableDeclaratorSyntax)
			{
				return new DescriptorWithArguments(Descriptors.ARDA006_UseStructuredLoggingToLogVariableValue, variableDeclaratorSyntax.Identifier.ValueText);
			}

			if (syntaxNode is PropertyDeclarationSyntax propertyDeclarationSyntax)
			{
				return new DescriptorWithArguments(Descriptors.ARDA007_UseStructuredLoggingToLogPropertyValue, propertyDeclarationSyntax.Identifier.ValueText);
			}

			if (syntaxNode is InvocationExpressionSyntax invocationExpressionSyntax)
			{
				string methodName = invocationExpressionSyntax.Expression.DescendantNodesAndSelf()
					.OfType<IdentifierNameSyntax>()
					.LastOrDefault()?
					.Identifier.ValueText;
				return new DescriptorWithArguments(Descriptors.ARDA008_UseStructuredLoggingToLogReturnValue, string.IsNullOrEmpty(methodName) ? "calling method" : methodName);
			}

			return diagnostic;
		}
	}
}

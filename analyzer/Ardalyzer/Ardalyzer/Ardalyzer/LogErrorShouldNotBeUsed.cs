using System.Collections.Immutable;
using Ardalyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Ardalyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LogErrorShouldNotBeUsed : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Descriptors.ARDA003_LogErrorShouldNotBeUsed);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterCompilationStartAction(compilationContext =>
			{
				var loggerType = compilationContext.Compilation.GetTypeByMetadataName(Constants.MicrosoftLoggerExtensionsType);
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

					if (methodSymbol.Name.Equals(Constants.LogErrorMethodName))
					{
						operationContext.ReportDiagnostic(
							Diagnostic.Create(
								Descriptors.ARDA003_LogErrorShouldNotBeUsed, 
								invocationOperation.Syntax.GetLocation()));
					}
				}, OperationKind.Invocation);
			});
		}
	}
}

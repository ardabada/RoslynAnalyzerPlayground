#define DEBUG_LoggingOfSensitiveData

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Ardalyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Ardalyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class LoggingOfSensitiveData : DiagnosticAnalyzer
	{
		private static readonly Regex SensitiveConfigPattern = new Regex("^(.+)\\((.*)\\)$", RegexOptions.Compiled, TimeSpan.FromSeconds(0.5));

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Descriptors.ARDA013_LoggingOfSensitiveData, Descriptors.ARDA014_SensitivePropertyIsLogged);

		public override void Initialize(AnalysisContext context)
		{
			AttachDebugger();

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();

			context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
		}

        [Conditional("DEBUG_" + nameof(LoggingOfSensitiveData))]
		private void AttachDebugger()
		{
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
        }

		private void AnalyzeInvocation(OperationAnalysisContext context)
		{
			var invocationOperation = (IInvocationOperation)context.Operation;
			var targetMethod = invocationOperation.TargetMethod;

			if (targetMethod.Name == "TestMethod")
			{
				var arguments = invocationOperation.Arguments;
				if (arguments.Length > 0 && IsParamsObjectArray(arguments[0].Parameter.Type))
				{
					var paramsObjectArray = (IArrayCreationOperation)arguments[0].Value;
					foreach (var argument in paramsObjectArray.Initializer.ElementValues)
					{
						var semanticModel = context.Compilation.GetSemanticModel(argument.Syntax.SyntaxTree);
						var argumentType = semanticModel.GetTypeInfo(argument.Syntax).Type;

						if (argumentType != null && ContainsSensitiveProperties(argumentType, context))
						{
							var diagnostic = Diagnostic.Create(Descriptors.ARDA013_LoggingOfSensitiveData, argument.Syntax.GetLocation());
							context.ReportDiagnostic(diagnostic);
						}
					}
				}
			}
		}

		private bool IsParamsObjectArray(ITypeSymbol typeSymbol)
		{
			if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
			{
				return arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_Object;
			}

			return false;
		}

		private bool ContainsSensitiveProperties(ITypeSymbol typeSymbol, OperationAnalysisContext context)
		{
			bool hasSensitiveData = false;
			foreach (var member in typeSymbol.GetMembers())
			{
				hasSensitiveData |= IsSensitiveMemberTree(member, context);
			}

			if (typeSymbol.BaseType != null)
            {
				hasSensitiveData |= ContainsSensitiveProperties(typeSymbol.BaseType, context);
			}

			return hasSensitiveData;
		}

		private bool IsSensitiveMemberTree(ISymbol member, OperationAnalysisContext context)
		{
			if (member is IPropertySymbol property)
			{
				if (property.Type.SpecialType == SpecialType.None)
				{
					return ContainsSensitiveProperties(property.Type, context);
				}
				else
                {
					var config = GetSensitivePropertiesConfiguration(property.Locations.First().SourceTree, context);
					foreach (var i in config)
					{
						if (CheckPropertySensivity(property, i))
						{
							var diagnostic = Diagnostic.Create(Descriptors.ARDA014_SensitivePropertyIsLogged, property.Locations.First(), property.Name);
							context.ReportDiagnostic(diagnostic);
							return true;
						}
					}
				}


				//else if (property.Type.SpecialType == SpecialType.System_String)
				//{
				//	if (property.Name.Equals("password", StringComparison.InvariantCultureIgnoreCase))
				//	{
				//		hasSensitiveData = true;
				//		var diagnostic = Diagnostic.Create(Descriptors.ARDA014_SensitivePropertyIsLogged, property.Locations.First(), property.Name);
				//		context.ReportDiagnostic(diagnostic);
				//	}
				//}
			}
			return false;
		}

		private bool CheckPropertySensivity(IPropertySymbol propertySymbol, (SpecialType SpecialType, string Pattern) config)
        {
			if (propertySymbol.Type.SpecialType == config.SpecialType)
            {
				var propertyName = propertySymbol.Name.ToLowerInvariant();
				Func<string, bool> comparisonFunc = propertyName.Equals;
				if (config.Pattern.StartsWith("%") && config.Pattern.EndsWith("%"))
				{
					comparisonFunc = propertyName.Contains;
				}
				else if (config.Pattern.StartsWith("%") && !config.Pattern.EndsWith("%"))
				{
					comparisonFunc = propertyName.StartsWith;
				}
				else if (!config.Pattern.StartsWith("%") && config.Pattern.EndsWith("%"))
                {
					comparisonFunc = propertyName.EndsWith;
				}

				return comparisonFunc(config.Pattern);
            }
			return false;
        }

		private IEnumerable<(SpecialType, string)> GetSensitivePropertiesConfiguration(SyntaxTree syntaxTree, OperationAnalysisContext context)
        {
			var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
			bool skipDefaultOptions = options.TryGetValue("arda.013.skip_default", out string skip);
			List<(SpecialType, string)?> result = new List<(SpecialType, string)?>();
			if (!skipDefaultOptions || skip == "false")
            {
				result.AddRange(Constants.DefaultSensitiveData.Select(x => Parse(x)));
            }
			var hasAdditionalItems = options.TryGetValue("arda.013.additional", out string additionalSensitiveFields);
			if (hasAdditionalItems && !string.IsNullOrEmpty(additionalSensitiveFields))
            {
				result.AddRange(additionalSensitiveFields.Split(',').Select(x => Parse(x)));
            }

			return result.Where(x => x != null).Cast<(SpecialType, string)>();

			(SpecialType, string)? Parse(string input)
            {
				var match = SensitiveConfigPattern.Match(input);
				if (!match.Success) return null;
				var name = match.Groups[1].Value.ToLowerInvariant();
				SpecialType type = match.Groups[2].Value switch
				{
					"string" => SpecialType.System_String,
					"int" => SpecialType.System_Int32,
					_ => SpecialType.None
				};
				return (type, name);
			}
        }
	}
}

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Ardalyzer.Utilities;

namespace Ardalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstantsShouldBeInPascalCase : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.ARDA002_ConstantsShouldBeInPascalCase);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

			context.RegisterSyntaxNodeAction(syntaxNodeContext =>
			{
                if (syntaxNodeContext.Node is not FieldDeclarationSyntax fieldDeclaration) return;

                bool isConst = fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword);
				if (!isConst) return;

				foreach (var variable in fieldDeclaration.Declaration.Variables)
				{
					string variableName = variable.Identifier.ValueText;
					bool isPascalCase = variableName.ToPascalCase().Equals(variableName);
					if (!isPascalCase)
					{
						syntaxNodeContext.ReportDiagnostic(
							Diagnostic.Create(
								Descriptors.ARDA002_ConstantsShouldBeInPascalCase, 
								variable.Identifier.GetLocation(), 
								variableName));
					}
				}
			}, SyntaxKind.FieldDeclaration);
		}
    }
}

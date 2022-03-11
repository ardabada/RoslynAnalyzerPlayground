using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Ardalyzer.Utilities;

namespace Ardalyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class GrpcFieldNameMustBeInSnakeCase : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create(Descriptors.ARDA010_GrpcFieldNameMustBeInSnakeCase);

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationAction(compilationContext =>
			{
				ImmutableArray<AdditionalText> additionalFiles = compilationContext.Options.AdditionalFiles;

				foreach (var protoFile in additionalFiles.Where(x => x.Path.EndsWith(".proto")))
				{
					SourceText fileText = protoFile.GetText(compilationContext.CancellationToken);
					for (int i = 0; i < fileText.Lines.Count; i++)
					{
						var line = fileText.Lines[i];
						var match = Regex.Match(line.ToString(), @"\w+\s+([\w\d_]+)\s*=\s*\d+;");
						var name = match.Groups[1].Value;
						if (string.IsNullOrEmpty(name)) continue;

						if (name.Any(c => char.IsUpper(c)))
						{
							compilationContext.ReportDiagnostic(
								Diagnostic.Create(
									Descriptors.ARDA010_GrpcFieldNameMustBeInSnakeCase,
									Location.None,
									name, Path.GetFileName(protoFile.Path), i + 1));
						}
					}
				}
			});
		}
	}
}

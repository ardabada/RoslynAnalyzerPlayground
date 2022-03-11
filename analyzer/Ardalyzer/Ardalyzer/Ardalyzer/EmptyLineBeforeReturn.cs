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
    public class EmptyLineBeforeReturn : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.ARDA011_EmptyLineBeforeReturn);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(syntaxNodeContext =>
            {
                if (syntaxNodeContext.Node is not ReturnStatementSyntax returnStatementSyntax) return;

                bool isInsideBlock = returnStatementSyntax.Parent is BlockSyntax;
                if (isInsideBlock)
                {
                    var nodesInsideBlock = returnStatementSyntax.Parent.ChildNodes().ToList();
                    bool hasEmptyLineBefore = returnStatementSyntax.ReturnKeyword.HasLeadingTrivia && returnStatementSyntax.ReturnKeyword.LeadingTrivia.Count(x => x.Kind() == SyntaxKind.EndOfLineTrivia) == 1;
                    var indexInsideBlock = nodesInsideBlock.IndexOf(returnStatementSyntax);
                    if (indexInsideBlock > 0)
                    {
                        var previousElementInsideBlock = nodesInsideBlock.ElementAt(indexInsideBlock - 1);
                        bool previousElementInsideBlockHasEndOfLine = previousElementInsideBlock.HasTrailingTrivia && previousElementInsideBlock.GetTrailingTrivia().Any(x => x.Kind() == SyntaxKind.EndOfLineTrivia);

                        if (!hasEmptyLineBefore || !previousElementInsideBlockHasEndOfLine)
                        {
                            syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(Descriptors.ARDA011_EmptyLineBeforeReturn, returnStatementSyntax.GetLocation()));
                        }
                    }
                    else
                    {
                        var parentBlock = returnStatementSyntax.Parent as BlockSyntax;
                        if (!(parentBlock.OpenBraceToken.TrailingTrivia.Count(x => x.Kind() == SyntaxKind.EndOfLineTrivia) == 1 && !hasEmptyLineBefore))
                        {
                            syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(Descriptors.ARDA011_EmptyLineBeforeReturn, returnStatementSyntax.GetLocation()));
                        }
                    }
                }
            }, SyntaxKind.ReturnStatement);
        }
    }
}

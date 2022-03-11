using System.Collections.Immutable;
using System.Composition;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ardalyzer.Utilities;

namespace Ardalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LogMessageFixer)), Shared]
    public class LogMessageFixer : CodeFixProvider
    {
        private const string Title = "Use structured logging";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.ARDA004_UseStructuredLoggingInsteadOfStringInterpolation.Id,
                Descriptors.ARDA005_UseStructuredLoggingToLogParameterValue.Id,
                Descriptors.ARDA006_UseStructuredLoggingToLogVariableValue.Id,
                Descriptors.ARDA007_UseStructuredLoggingToLogPropertyValue.Id,
                Descriptors.ARDA008_UseStructuredLoggingToLogReturnValue.Id);

        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var invocationExpressionSyntax = root.FindNode(context.Span) as InvocationExpressionSyntax;
            if (invocationExpressionSyntax is null) return;

            context.RegisterCodeFix(
               CodeAction.Create(
                   title: Title,
                   createChangedDocument: ct => FixLogMessage(context.Document, root, invocationExpressionSyntax, ct),
                   equivalenceKey: Title),
               context.Diagnostics);
        }

        public string ReplaceNode(SyntaxNode syntaxNode, SemanticModel semanticModel)
        {
            if (syntaxNode is null) return null;

            if (syntaxNode is BinaryExpressionSyntax binaryExpressionSyntax && 
                binaryExpressionSyntax.OperatorToken.IsKind(SyntaxKind.PlusToken))
            {
                return (ReplaceNode(binaryExpressionSyntax.Left, semanticModel) + " " + ReplaceNode(binaryExpressionSyntax.Right, semanticModel)).Trim();
            }

            if (syntaxNode is IdentifierNameSyntax ||
                syntaxNode is MemberAccessExpressionSyntax s)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
                var symbol = symbolInfo.Symbol;
                if (symbol is null) return null;

                var replacements = symbol.DeclaringSyntaxReferences
                    .Select(x => ReplaceNode(x.GetSyntax(), semanticModel))
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Select(x => x.Trim());
                return string.Join(" ", replacements);
            }

            if (syntaxNode is ParameterSyntax parameterSyntax)
            {
                return "{@" + parameterSyntax.Identifier.ValueText + "}";
            }

            if (syntaxNode is VariableDeclaratorSyntax variableDeclaratorSyntax)
            {
                return "{@" + variableDeclaratorSyntax.Identifier.ValueText + "}";
            }

            if (syntaxNode is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                string methodName = invocationExpressionSyntax.Expression.DescendantNodesAndSelf()
                    .OfType<IdentifierNameSyntax>()
                    .LastOrDefault()?
                    .Identifier.ValueText;
                if (string.IsNullOrEmpty(methodName)) return null;

                return "{@" + methodName + "}";
            }
            if (syntaxNode is PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                return "{@" + propertyDeclarationSyntax.Identifier.ValueText + "}";
            }

            if (syntaxNode is InterpolatedStringExpressionSyntax interpolatedStringExpressionSyntax)
            {
                List<string> interpolated = new List<string>();
                foreach (var i in interpolatedStringExpressionSyntax.Contents)
                {
                    if (i is InterpolationSyntax interpolationSyntax)
                    {
                        interpolated.Add(ReplaceNode(interpolationSyntax.Expression, semanticModel));
                    }
                    if (i is InterpolatedStringTextSyntax interpolatedStringTextSyntax)
                    {
                        interpolated.Add(interpolatedStringTextSyntax.TextToken.ValueText);
                    }
                }
                return string.Join(" ", interpolated.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()));
            }

            if (syntaxNode is LiteralExpressionSyntax literalExpressionSyntax)
            {
                return literalExpressionSyntax.Token.ValueText;
            }

            return null;
        }

        private async Task<Document> FixLogMessage(Document document, SyntaxNode root, InvocationExpressionSyntax invocationExpressionSyntax, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            Queue<ArgumentSyntax> logArguments = new Queue<ArgumentSyntax>();

            int initialIndex = 1;
            var messageExpression = invocationExpressionSyntax.ArgumentList.Arguments[initialIndex].Expression;

            string logFormat = ReplaceNode(messageExpression);
            SeparatedSyntaxList<ArgumentSyntax> args = new SeparatedSyntaxList<ArgumentSyntax>();
            args = args.Add(invocationExpressionSyntax.ArgumentList.Arguments[0]);
            args = args.Add(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(logFormat))));
            args = args.AddRange(logArguments);
            var newInvocation = invocationExpressionSyntax.WithArgumentList(SyntaxFactory.ArgumentList(args));
            var newRoot = root.ReplaceNode(invocationExpressionSyntax, newInvocation);

            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;

            string ReplaceNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is null) return null;

                if (syntaxNode is BinaryExpressionSyntax binaryExpressionSyntax &&
                    binaryExpressionSyntax.OperatorToken.IsKind(SyntaxKind.PlusToken))
                {
                    return (ReplaceNode(binaryExpressionSyntax.Left) + " " + ReplaceNode(binaryExpressionSyntax.Right)).Trim();
                }

                if (syntaxNode is IdentifierNameSyntax ||
                    syntaxNode is MemberAccessExpressionSyntax)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
                    var symbol = symbolInfo.Symbol;
                    if (symbol is null) return null;

                    logArguments.Enqueue(SyntaxFactory.Argument(syntaxNode as ExpressionSyntax));
                    return "{@" + symbol.Name + "}";
                }

                if (syntaxNode is InvocationExpressionSyntax invocationExpressionSyntax)
                {
                    string methodName = invocationExpressionSyntax.Expression.DescendantNodesAndSelf()
                        .OfType<IdentifierNameSyntax>()
                        .LastOrDefault()?
                        .Identifier.ValueText;
                    if (string.IsNullOrEmpty(methodName)) return null;

                    logArguments.Enqueue(SyntaxFactory.Argument(invocationExpressionSyntax));
                    return "{@" + methodName + "}";
                }

                if (syntaxNode is InterpolatedStringExpressionSyntax interpolatedStringExpressionSyntax)
                {
                    List<string> interpolated = new List<string>();
                    foreach (var i in interpolatedStringExpressionSyntax.Contents)
                    {
                        if (i is InterpolationSyntax interpolationSyntax)
                        {
                            interpolated.Add(ReplaceNode(interpolationSyntax.Expression));
                        }
                        if (i is InterpolatedStringTextSyntax interpolatedStringTextSyntax)
                        {
                            string originalText = interpolatedStringTextSyntax.TextToken.ValueText;
                            EnsureStringArgumentsOrder(originalText);
                            interpolated.Add(originalText);
                        }
                    }
                    return string.Join(" ", interpolated.Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim()));
                }

                if (syntaxNode is LiteralExpressionSyntax literalExpressionSyntax)
                {
                    string originalText = literalExpressionSyntax.Token.ValueText;
                    EnsureStringArgumentsOrder(originalText);
                    return originalText;
                }

                return null;
            }

            void EnsureStringArgumentsOrder(string originalText)
            {
                int argumentsToSkip = Regex.Matches(originalText, @"\{[\w\d@_-]+\}").Count;
                for (int i = 1; i <= argumentsToSkip; i++)
                {
                    int index = initialIndex + logArguments.Count + i;
                    if (invocationExpressionSyntax.ArgumentList.Arguments.Count <= index) break;

                    var argument = invocationExpressionSyntax.ArgumentList.Arguments[index];
                    logArguments.Enqueue(argument);
                }
            }
        }
    }
}

using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Ardalyzer.Utilities.Category;

namespace Ardalyzer.Utilities
{
    public enum Category
    {
        Conventions,
        Naming,
        Usage
    }

    public static class Descriptors
    {
        private static readonly ConcurrentDictionary<Category, string> categoryMapping = new ConcurrentDictionary<Category, string>();

        static DiagnosticDescriptor Rule(string id, string title, Category category, DiagnosticSeverity defaultSeverity, string messageFormat)
        {
            var helpLink = $"https://google.com/search?q={id}";
            var categoryString = categoryMapping.GetOrAdd(category, c => c.ToString());

            return new DiagnosticDescriptor(id, title, messageFormat, categoryString, defaultSeverity, isEnabledByDefault: true, helpLinkUri: helpLink);
        }

        public static DiagnosticDescriptor ARDA001_NamespaceShouldStartWithArdabadaPlayground { get; } =
            Rule(
                "ARDA001",
                $"Namespace should start with {Constants.NamespacePrefix}",
                Naming,
                Warning,
                $"Namespace \"{{0}}\" should start with {Constants.NamespacePrefix}");

        public static DiagnosticDescriptor ARDA002_ConstantsShouldBeInPascalCase { get; } =
            Rule(
                "ARDA002",
                "Constants should be in PascalCase",
                Naming,
                Warning,
                "Constant \"{0}\" should be in PascalCase");

        public static DiagnosticDescriptor ARDA003_LogErrorShouldNotBeUsed { get; } =
            Rule(
                "ARDA003",
                "LogError should not be used",
                Naming,
                Warning,
                "LogError should not be used. Use LogErrorWithCode instead");

        public static DiagnosticDescriptor ARDA004_UseStructuredLoggingInsteadOfStringInterpolation { get; } =
            Rule(
                "ARDA004",
                "Use structured logging",
                Usage,
                Warning,
                "Use structured logging instead of string interpolation");

        public static DiagnosticDescriptor ARDA005_UseStructuredLoggingToLogParameterValue { get; } =
            Rule(
                "ARDA005",
                "Use structured logging",
                Usage,
                Warning,
                "Use structured logging to log value of parameter \"{0}\"");

        public static DiagnosticDescriptor ARDA006_UseStructuredLoggingToLogVariableValue { get; } =
            Rule(
                "ARDA006",
                "Use structured logging",
                Usage,
                Warning,
                "Use structured logging to log value of variable \"{0}\"");

        public static DiagnosticDescriptor ARDA007_UseStructuredLoggingToLogPropertyValue { get; } =
            Rule(
                "ARDA007",
                "Use structured logging",
                Usage,
                Warning,
                "Use structured logging to log value of property \"{0}\"");

        public static DiagnosticDescriptor ARDA008_UseStructuredLoggingToLogReturnValue { get; } =
            Rule(
                "ARDA008",
                "Use structured logging",
                Usage,
                Warning,
                "Use structured logging to log return value of \"{0}\"");

        public static DiagnosticDescriptor ARDA009_LogMessageShouldBeConstant { get; } =
            Rule(
                "ARDA009",
                "Log message should be a constant",
                Usage,
                Warning,
                "Log message should be a constant");

        public static DiagnosticDescriptor ARDA010_GrpcFieldNameMustBeInSnakeCase { get; } =
            Rule(
                "ARDA010",
                "gRPC field name must be in snake_case",
                Naming,
                Warning,
                "gRPC field \"{0}\" in file \"{1}\" on line {2} must be in snake_case");

        public static DiagnosticDescriptor ARDA011_EmptyLineBeforeReturn { get; } =
            Rule(
                "ARDA011",
                "Empty line before return",
                Conventions,
                Warning,
                "Place one empty line before return.");

        public static DiagnosticDescriptor ARDA012_ClassNotAddedToDependencyInjectionContainer { get; } =
            Rule(
                "ARDA011",
                "Class not added to dependency injection container",
                Usage,
                Error,
                "Class \"{0}\" not added to dependency injection container");
    }
}

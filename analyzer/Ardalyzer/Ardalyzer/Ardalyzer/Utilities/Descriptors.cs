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
                $"Namespace should start with {NamespaceShouldStartWithArdabadaPlayground.ExpectedNamespace}",
                Naming,
                Warning,
                $"Namespace \"{{0}}\" should start with {NamespaceShouldStartWithArdabadaPlayground.ExpectedNamespace}");

    }
}

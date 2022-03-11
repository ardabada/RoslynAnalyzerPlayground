using Microsoft.CodeAnalysis;

namespace Ardalyzer.Utilities
{
    public class DescriptorWithArguments
    {
        public DescriptorWithArguments(DiagnosticDescriptor descriptor, params object[] args)
        {
            this.Descriptor = descriptor;
            this.Arguments = args;
        }

        public DiagnosticDescriptor Descriptor { get; }
        public object[] Arguments { get; }
    }
}

﻿using System.Threading.Tasks;
using Xunit;
using VerifyCS = Ardalyzer.Test.CSharpCodeFixVerifier<
    Ardalyzer.NamespaceShouldStartWithArdabadaPlayground, 
    Ardalyzer.NamespaceShouldStartWithArdabadaPlaygroundFixer>;

namespace Ardalyzer.Test.Analyzers
{
    public class NamespaceShouldStartWithArdabadaPlaygroundTests
    {
        [Fact]
        public async Task NamespaceStartsWithArdabadaPlayground_NoDiagnostics()
        {
            var test = @"namespace Ardabada.Playground.Sample { }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task NamespaceEqualsArdabadaPlayground_NoDiagnostics()
        {
            var test = @"namespace Ardabada.Playground { }";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task NamespaceDoestNotStartWithArdabadaPlayground_ShouldReportDiagnostics()
        {
            var before = @"using System;

namespace Sample
{
    class Test { }
}";
            var after = @"using System;

namespace Ardabada.Playground.Sample
{
    class Test { }
}";
            var expected = VerifyCS
                .Diagnostic()
                .WithSpan(3, 11, 3, 17)
                .WithMessage("Namespace \"Sample\" should start with Ardabada.Playground");

            await VerifyCS.VerifyCodeFixAsync(before, expected, after);
        }

        [Fact]
        public async Task CodeFix_ShouldPreserveLeadingAndEndingTrivia()
        {
            var before = @"namespace   Sample  { }";
            var after = @"namespace   Ardabada.Playground.Sample  { }";
            var expected = VerifyCS
                .Diagnostic()
                .WithSpan(1, 13, 1, 19)
                .WithMessage("Namespace \"Sample\" should start with Ardabada.Playground");

            await VerifyCS.VerifyCodeFixAsync(before, expected, after);
        }
    }
}

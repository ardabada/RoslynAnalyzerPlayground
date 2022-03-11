using System.Threading.Tasks;
using Ardalyzer.Utilities;
using Xunit;
using VerifyCS = Ardalyzer.Test.CSharpCodeFixVerifier<
    Ardalyzer.LogMessageAnalyzer,
    Ardalyzer.LogMessageFixer>;

namespace Ardalyzer.Test
{
    public class LogMessageTests
    {
        [Fact]
        public async Task BinaryNonPlusExpression_DiagnosticsReported()
        {
            var test = $@"using Microsoft.Extensions.Logging;
using Playground.Utils.Extensions;

class Test
{{
    ILogger<Test> logger;

    public void Sample(bool flag)
    {{
        logger.LogErrorWithCode(""code"", flag ? ""x"" : ""y""); 
    }}
}}";
            var expected = VerifyCS
                .Diagnostic(Descriptors.ARDA009_LogMessageShouldBeConstant.Id)
                .WithSpan(10, 9, 10, 58)
                .WithMessage("Log message should be a constant");
            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [Fact]
        public async Task BinaryConcatenation_DiagnosticsReported()
        {
            var before = $@"using Microsoft.Extensions.Logging;
using Playground.Utils.Extensions;

class Test
{{
    ILogger<Test> logger;

    public void Sample(bool flag)
    {{
        string test = ""abc"";
        logger.LogErrorWithCode(""code"", test + ""123""); 
    }}
}}";
            var after = $@"using Microsoft.Extensions.Logging;
using Playground.Utils.Extensions;

class Test
{{
    ILogger<Test> logger;

    public void Sample(bool flag)
    {{
        string test = ""abc"";
        logger.LogErrorWithCode(""code"", ""{{@test}} 123"", test); 
    }}
}}";
            var expected = VerifyCS
                .Diagnostic(Descriptors.ARDA006_UseStructuredLoggingToLogVariableValue.Id)
                .WithSpan(11, 9, 11, 54)
                .WithMessage("Use structured logging to log value of variable \"test\"");
            await VerifyCS.VerifyCodeFixAsync(before, expected, after);
        }
    }
}

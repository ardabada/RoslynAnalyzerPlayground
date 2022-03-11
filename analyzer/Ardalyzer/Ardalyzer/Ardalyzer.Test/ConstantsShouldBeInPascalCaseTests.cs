using System.Threading.Tasks;
using Xunit;
using VerifyCS = Ardalyzer.Test.CSharpCodeFixVerifier<
    Ardalyzer.ConstantsShouldBeInPascalCase,
    Ardalyzer.ConstantsShouldBeInPascalCaseFixer>;


namespace Ardalyzer.Test
{
    public class ConstantsShouldBeInPascalCaseTests
    {
        [Theory]
        [InlineData("Test")]
        [InlineData("AValue")]
        [InlineData("AValuePascalCase")]
        [InlineData("PascalCase")]
        public async Task ConstantIsInPascalCase_NoDiagnostics(string constantName)
        {
            var test = $@"class Sample
{{
    const int {constantName} = 10;
}}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Theory]
        [InlineData("test", "Test")]
        [InlineData("ABValue", "AbValue")]
        [InlineData("TEST", "Test")]
        [InlineData("Test_Name", "TestName")]
        [InlineData("test_name", "TestName")]
        [InlineData("testName", "TestName")]
        public async Task ConstantIsNotInPascalCase_ShouldReportDiagnostics(string constantName, string fixedName)
        {
            var before = $@"class Sample
{{
    const int {constantName} = 10;
}}";
            var after = $@"class Sample
{{
    const int {fixedName} = 10;
}}";

            var expected = VerifyCS
                .Diagnostic()
                .WithSpan(3, 15, 3, 15 + constantName.Length)
                .WithMessage($"Constant \"{constantName}\" should be in PascalCase");

            await VerifyCS.VerifyCodeFixAsync(before, expected, after);
        }

        [Fact]
        public async Task MultipleInlineConstants_ValidNames_NoDiagnostics()
        {
            var test = $@"class Sample
{{
    const int TestValue1 = 5, TestValue2 = 10;
}}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task MultipleInlineConstants_InvalidNames_ShouldReportDiagnostics()
        {
            var before = $@"class Sample
{{
    const int TestValue1 = 5, INVALID_Value = 1, TestValue2 = 10;
}}";
            var after = $@"class Sample
{{
    const int TestValue1 = 5, InvalidValue = 1, TestValue2 = 10;
}}";

            var expected1 = VerifyCS
                .Diagnostic()
                .WithSpan(3, 31, 3, 44)
                .WithMessage($"Constant \"INVALID_Value\" should be in PascalCase");

            await VerifyCS.VerifyCodeFixAsync(before, expected1, after);
        }
    }
}

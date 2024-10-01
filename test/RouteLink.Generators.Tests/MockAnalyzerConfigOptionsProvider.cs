using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RouteLink.Generators.Tests;

public partial class ComponentsRouteGeneratorTests
{
    public class MockAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; }

        public MockAnalyzerConfigOptionsProvider(IDictionary<string, string> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            GlobalOptions = new MockAnalyzerConfigOptions(options);
        }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return GlobalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return GlobalOptions;
        }
    }
}

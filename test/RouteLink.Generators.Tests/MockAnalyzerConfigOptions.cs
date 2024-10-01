using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis.Diagnostics;

namespace RouteLink.Generators.Tests;

public partial class ComponentsRouteGeneratorTests
{
    public class MockAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IDictionary<string, string> _options;

        public MockAnalyzerConfigOptions(IDictionary<string, string> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            return _options.TryGetValue(key, out value);
        }
    }
}

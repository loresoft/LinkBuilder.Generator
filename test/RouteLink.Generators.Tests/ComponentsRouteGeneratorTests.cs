using System.Collections.Immutable;

using FluentAssertions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RouteLink.Generators.Tests;

public partial class ComponentsRouteGeneratorTests
{
    [Fact]
    public Task GenerateWithoutParameter()
    {
        var source = """
using Microsoft.AspNetCore.Components;

namespace RouteLink.Generators.Tests.Pages.Administration.ContactTypes;

[Route("/administration/contactTypes")]
public class List { }
""";

        var options = new Dictionary<string, string>
        {
            ["build_property.RoutesNamespace"] = "RouteLink.Generators.Tests",
            ["build_property.RoutesClassName"] = "Routes"
        };

        var (diagnostics, output) = GetGeneratedOutput<ComponentsRouteGenerator>(source, options);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateWithParameterConstraint()
    {
        var source = """
using Microsoft.AspNetCore.Components;

namespace RouteLink.Generators.Tests.Pages.Administration.ContactTypes;

[Route("/administration/contactTypes/{id:int}")]
public class Edit { }
""";

        var options = new Dictionary<string, string>
        {
            ["build_property.RoutesNamespace"] = "RouteLink.Generators.Tests",
            ["build_property.RoutesClassName"] = "Routes"
        };

        var (diagnostics, output) = GetGeneratedOutput<ComponentsRouteGenerator>(source, options);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    private static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput<T>(string source, IDictionary<string, string>? options = null)
        where T : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ComponentsRouteGenerator).Assembly.Location),
            ]);

        var compilation = CSharpCompilation.Create(
            assemblyName: "RouteLink.Generators.Tests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var originalTreeCount = compilation.SyntaxTrees.Length;
        var generator = new T();


        var driver = CSharpGeneratorDriver.Create(generator) as GeneratorDriver;

        if (options != null)
        {
            var analyzerOptionsProvider = new MockAnalyzerConfigOptionsProvider(options);
            driver = driver.WithUpdatedAnalyzerConfigOptions(analyzerOptionsProvider);
        }

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var trees = outputCompilation.SyntaxTrees.ToList();

        return (diagnostics, trees.Count != originalTreeCount ? trees[^1].ToString() : string.Empty);
    }
}

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using RouteLink.Generators.Models;
using RouteLink.Generators.Parsing;

namespace RouteLink.Generators;

[Generator]
public class ComponentsRouteGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "Microsoft.AspNetCore.Components.RouteAttribute",
            predicate: SyntacticPredicate,
            transform: SemanticTransform
        )
        .Where(static context => context is not null);

        var diagnostics = provider
            .Select(static (item, _) => item?.Diagnostics)
            .Where(static item => item?.Count > 0);

        context.RegisterSourceOutput(diagnostics, ReportDiagnostic);

        // output code
        var linkContexts = provider
            .Select(static (item, _) => item?.LinkContext)
            .Where(static item => item is not null);

        var routeOptions = context.AnalyzerConfigOptionsProvider
            .Select(static (c, _) =>
            {
                c.GlobalOptions.TryGetValue("build_property.AssemblyName", out var assemblyName);
                c.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                c.GlobalOptions.TryGetValue("build_property.RoutesNamespace", out var routesNamespace);
                c.GlobalOptions.TryGetValue("build_property.RoutesClassName", out var routesClassName);

                return new RouteOptions(
                    routesNamespace ?? rootNamespace ?? assemblyName ?? "RouteLink",
                    routesClassName ?? "Routes"
                );
            });

        context.RegisterSourceOutput(linkContexts.Combine(routeOptions), GenerateOutput);
    }

    private void GenerateOutput(SourceProductionContext context, (LinkContext? LinkContext, RouteOptions RouteOptions) parameters)
    {
        var linkContext = parameters.LinkContext;
        var options = parameters.RouteOptions;

        if (linkContext == null)
            return;

        var fileName = RoutePath(linkContext, options);
        var source = RouteLinkWriter.GenerateLink(options, linkContext);

        context.AddSource(fileName, source);
    }

    private static string RoutePath(LinkContext linkContext, RouteOptions options)
    {
        var routeNamespances = options.RoutesNamespace.Split('.');

        var builder = new StringBuilder("Routes");
        for (var i = 0; i < linkContext.Types.Count; i++)
        {
            if (i < routeNamespances.Length && routeNamespances[i] == linkContext.Types[i])
                continue;

            builder
                .Append('.')
                .Append(linkContext.Types[i]);
        }

        builder.Append('.');

        // hash link names to prevent duplicate file names
        for (var i = 0; i < linkContext.Routes.Count; i++)
            builder.Append(linkContext.Routes[0].Name);

        builder.Append(".g.cs");

        return builder.ToString();
    }

    private static void ReportDiagnostic(SourceProductionContext context, EquatableArray<Diagnostic>? diagnostics)
    {
        if (diagnostics == null)
            return;

        foreach (var diagnostic in diagnostics)
            context.ReportDiagnostic(diagnostic);
    }

    private static bool SyntacticPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax;
    }

    private static GeneratorContext? SemanticTransform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not INamedTypeSymbol targetSymbol)
            return null;

        var diagnostics = new List<Diagnostic>();

        var className = targetSymbol.Name;

        // support nested types
        var containingNamespaces = GetContainingNamespaces(targetSymbol);

        var linkTemplates = new List<LinkTemplate>();
        foreach (var attribute in context.Attributes)
        {
            if (attribute == null || attribute.ConstructorArguments.Length != 1)
                continue;

            var comparerArgument = attribute.ConstructorArguments[0];
            if (comparerArgument.Value is not string templateString)
                continue;

            var pattern = TemplatePatternParser.Parse(templateString);

            var linkTemplate = new LinkTemplate(className, pattern);
            linkTemplates.Add(linkTemplate);
        }

        var linkContext = new LinkContext(containingNamespaces, linkTemplates);

        return new GeneratorContext(linkContext, diagnostics.ToArray());
    }

    private static EquatableArray<string> GetContainingNamespaces(INamedTypeSymbol targetSymbol)
    {
        if (targetSymbol.ContainingNamespace is null)
            return Array.Empty<string>();

        var containingTypes = new List<string>();
        var currentSymbol = targetSymbol.ContainingNamespace;

        while (currentSymbol != null)
        {
            if (!string.IsNullOrEmpty(currentSymbol.Name))
                containingTypes.Add(currentSymbol.Name);

            currentSymbol = currentSymbol.ContainingNamespace;
        }

        containingTypes.Reverse();

        return containingTypes.ToArray();
    }
}

using Microsoft.CodeAnalysis;

namespace RouteLink.Generators.Models;

public record GeneratorContext(
    LinkContext? LinkContext,
    EquatableArray<Diagnostic>? Diagnostics
);

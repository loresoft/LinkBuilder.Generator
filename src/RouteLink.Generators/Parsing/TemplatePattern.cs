using RouteLink.Generators.Models;

namespace RouteLink.Generators.Parsing;

public record TemplatePattern(
    string TemplateText,
    EquatableArray<TemplatePart> Segments,
    EquatableArray<string> Errors
);

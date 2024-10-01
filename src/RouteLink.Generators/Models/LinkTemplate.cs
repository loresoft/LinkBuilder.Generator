using RouteLink.Generators.Parsing;

namespace RouteLink.Generators.Models;

public record LinkTemplate(
    string Name,
    TemplatePattern Template
);

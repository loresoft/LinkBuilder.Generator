namespace RouteLink.Generators.Models;

public record LinkContext(
    EquatableArray<string> Types,
    EquatableArray<LinkTemplate> Routes
);

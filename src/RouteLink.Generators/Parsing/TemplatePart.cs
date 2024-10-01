using RouteLink.Generators.Models;

namespace RouteLink.Generators.Parsing;

public record TemplatePart
{
    public TemplatePart(string text)
    {
        Text = text;
        Name = null;
        IsCatchAll = false;
        IsLiteral = true;
        IsParameter = false;
        IsOptional = false;
        DefaultValue = null;
        Constraints = [];
    }

    public TemplatePart(
        string text,
        string? name,
        bool isCatchAll,
        bool isOptional,
        object? defaultValue,
        EquatableArray<string> constraints
    )
    {
        Text = text;
        Name = name;
        IsCatchAll = isCatchAll;
        IsLiteral = false;
        IsParameter = true;
        IsOptional = isOptional;
        DefaultValue = defaultValue;
        Constraints = constraints;
    }

    public string Text { get; init; }

    public string? Name { get; init; }

    public bool IsCatchAll { get; init; }

    public bool IsLiteral { get; init; }

    public bool IsParameter { get; init; }

    public bool IsOptional { get; init; }

    public object? DefaultValue { get; init; }

    public EquatableArray<string> Constraints { get; init; }
}

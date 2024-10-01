using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentAssertions;

using RouteLink.Generators.Parsing;

using Xunit;

namespace RouteLink.Generators.Tests.Parsing;

public class TemplateParserTests
{
    [Fact]
    public void RootPath()
    {
        var routeTemplate = TemplatePatternParser.Parse("/");

        routeTemplate.Should().NotBeNull();
        routeTemplate.TemplateText.Should().Be("/");
        routeTemplate.Segments.Should().NotBeNull();
        routeTemplate.Segments.Count.Should().Be(1);
    }

    [Fact]
    public void NoPath()
    {
        var routeTemplate = TemplatePatternParser.Parse("");

        routeTemplate.Should().NotBeNull();
        routeTemplate.TemplateText.Should().Be("/");
        routeTemplate.Segments.Should().NotBeNull();
        routeTemplate.Segments.Count.Should().Be(1);
    }

    [Fact]
    public void ParameterOptionalWithConstraint()
    {
        var routeTemplate = TemplatePatternParser.Parse("/Home/Index/{id:int?}");

        routeTemplate.Should().NotBeNull();

        routeTemplate.Segments.Should().NotBeNull();
        routeTemplate.Segments.Count.Should().Be(3);

        routeTemplate.Segments[0].Text.Should().Be("Home");
        routeTemplate.Segments[0].IsParameter.Should().BeFalse();
        routeTemplate.Segments[0].IsOptional.Should().BeFalse();
        routeTemplate.Segments[0].IsCatchAll.Should().BeFalse();

        routeTemplate.Segments[1].Text.Should().Be("Index");

        routeTemplate.Segments[2].Name.Should().Be("id");
        routeTemplate.Segments[2].IsParameter.Should().BeTrue();
        routeTemplate.Segments[2].IsOptional.Should().BeTrue();
        routeTemplate.Segments[2].IsCatchAll.Should().BeFalse();

        routeTemplate.Segments[2].Constraints.Should().NotBeNull();
        routeTemplate.Segments[2].Constraints.Count.Should().Be(1);
        routeTemplate.Segments[2].Constraints[0].Should().Be("int");
    }

    [Fact]
    public void ParameterWithoutConstraint()
    {
        var routeTemplate = TemplatePatternParser.Parse("/Home/Index/{id}");

        routeTemplate.Should().NotBeNull();

        routeTemplate.Segments.Should().NotBeNull();
        routeTemplate.Segments.Count.Should().Be(3);

        routeTemplate.Segments[0].Text.Should().Be("Home");
        routeTemplate.Segments[0].IsParameter.Should().BeFalse();
        routeTemplate.Segments[0].IsOptional.Should().BeFalse();
        routeTemplate.Segments[0].IsCatchAll.Should().BeFalse();

        routeTemplate.Segments[1].Text.Should().Be("Index");

        routeTemplate.Segments[2].Name.Should().Be("id");
        routeTemplate.Segments[2].IsParameter.Should().BeTrue();
        routeTemplate.Segments[2].IsOptional.Should().BeFalse();
        routeTemplate.Segments[2].IsCatchAll.Should().BeFalse();

        routeTemplate.Segments[2].Constraints.Should().NotBeNull();
        routeTemplate.Segments[2].Constraints.Count.Should().Be(0);
    }

    [Fact]
    public void ParameterCatchAll()
    {
        var routeTemplate = TemplatePatternParser.Parse("/Home/Index/{*catchAll}");

        routeTemplate.Should().NotBeNull();

        routeTemplate.Segments.Should().NotBeNull();
        routeTemplate.Segments.Count.Should().Be(3);

        routeTemplate.Segments[0].Text.Should().Be("Home");
        routeTemplate.Segments[0].IsParameter.Should().BeFalse();
        routeTemplate.Segments[0].IsOptional.Should().BeFalse();
        routeTemplate.Segments[0].IsCatchAll.Should().BeFalse();

        routeTemplate.Segments[1].Text.Should().Be("Index");

        routeTemplate.Segments[2].Name.Should().Be("catchAll");
        routeTemplate.Segments[2].IsParameter.Should().BeTrue();
        routeTemplate.Segments[2].IsOptional.Should().BeFalse();
        routeTemplate.Segments[2].IsCatchAll.Should().BeTrue();

        routeTemplate.Segments[2].Constraints.Should().NotBeNull();
        routeTemplate.Segments[2].Constraints.Count.Should().Be(0);
    }

    [Theory]
    [InlineData("/Home/{id:alpha}", "alpha")]
    [InlineData("/Home/{id:bool}", "bool")]
    [InlineData("/Home/{id:datetime}", "datetime")]
    [InlineData("/Home/{id:decimal}", "decimal")]
    [InlineData("/Home/{id:double}", "double")]
    [InlineData("/Home/{id:float}", "float")]
    [InlineData("/Home/{id:guid}", "guid")]
    [InlineData("/Home/{id:int}", "int")]
    [InlineData("/Home/{id:long}", "long")]
    [InlineData("/Home/{id:length(6)}", "length(6)")]
    [InlineData("/Home/{id:max(10)}", "max(10)")]
    [InlineData("/Home/{id:maxlength(10)}", "maxlength(10)")]
    [InlineData("/Home/{id:min(10)}", "min(10)")]
    [InlineData("/Home/{id:minlength(10)}", "minlength(10)")]
    [InlineData("/Home/{id:range(10,50)}", "range(10,50)")]
    [InlineData("/Home/{id:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)}", "regex(^\\d{3}-\\d{2}-\\d{4}$)")]
    public void ParameterWithConstraint(string template, string constraint)
    {
        var routeTemplate = TemplatePatternParser.Parse(template);

        routeTemplate.Should().NotBeNull();

        routeTemplate.Segments.Should().NotBeNull();
        routeTemplate.Segments.Count.Should().Be(2);

        routeTemplate.Segments[0].Text.Should().Be("Home");
        routeTemplate.Segments[0].IsParameter.Should().BeFalse();
        routeTemplate.Segments[0].IsOptional.Should().BeFalse();
        routeTemplate.Segments[0].IsCatchAll.Should().BeFalse();

        routeTemplate.Segments[1].Name.Should().Be("id");
        routeTemplate.Segments[1].IsParameter.Should().BeTrue();
        routeTemplate.Segments[1].IsOptional.Should().BeFalse();
        routeTemplate.Segments[1].IsCatchAll.Should().BeFalse();

        routeTemplate.Segments[1].Constraints.Should().NotBeNull();
        routeTemplate.Segments[1].Constraints.Count.Should().Be(1);
        routeTemplate.Segments[1].Constraints[0].Should().Be(constraint);
    }
}

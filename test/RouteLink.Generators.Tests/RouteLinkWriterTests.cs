using System;

using RouteLink.Generators.Models;
using RouteLink.Generators.Parsing;

using Xunit;

namespace RouteLink.Generators.Tests;

public class RouteLinkWriterTests
{
    [Fact]
    public async Task GenerateBasic()
    {
        var options = new RouteOptions("RouteLink.Generators.Tests", "Routes");
        var linkContext = new LinkContext(
            Types: new EquatableArray<string>(["Pages", "Clients"]),
            Routes: new EquatableArray<LinkTemplate>([
                new LinkTemplate(
                    Name: "List",
                    Template: new TemplatePattern(
                        TemplateText: "/clients",
                        Segments: new EquatableArray<TemplatePart>([
                            new TemplatePart(
                                text: "client"
                            )
                        ]),
                        Errors: []
                    )
                )
            ])
        );

        var output = RouteLinkWriter.GenerateLink(options, linkContext);

        await
            Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public async Task GenerateBasicParameters()
    {
        var options = new RouteOptions("RouteLink.Generators.Tests", "Routes");
        var linkContext = new LinkContext(
            Types: new EquatableArray<string>(["Pages", "Clients"]),
            Routes: new EquatableArray<LinkTemplate>([
                new LinkTemplate(
                    Name: "Edit",
                    Template:  new TemplatePattern(
                        TemplateText: "/clients/{id:int}",
                        Segments: new EquatableArray<TemplatePart>([
                            new TemplatePart(
                                text: "client"
                            ),
                            new TemplatePart(
                                text: "{id}",
                                name: "id",
                                isCatchAll: false,
                                isOptional: false,
                                defaultValue: null,
                                constraints: []
                            )
                        ]),
                        Errors: []
                    )
                )
            ])
        );

        var output = RouteLinkWriter.GenerateLink(options, linkContext);

        await
            Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public async Task GenerateMultipleParameters()
    {
        var options = new RouteOptions("RouteLink.Generators.Tests", "Routes");
        var linkContext = new LinkContext(
            Types: new EquatableArray<string>(["Pages", "Clients", "Facility"]),
            Routes: new EquatableArray<LinkTemplate>([
                new LinkTemplate(
                    Name: "FacilityEdit",
                    Template: new TemplatePattern(
                        TemplateText: "/clients/{clientId:int}/facilities/{facilityId:int}",
                        Segments: new EquatableArray<TemplatePart>([
                            new TemplatePart(
                                text: "client"
                            ),
                            new TemplatePart(
                                text: "{clientId}",
                                name: "clientId",
                                isCatchAll: false,
                                isOptional: false,
                                defaultValue: null,
                                constraints: new EquatableArray<string>(["int"])
                            ),
                            new TemplatePart(
                                text: "facilities"
                            ),
                            new TemplatePart(
                                text: "{facilityId}",
                                name: "facilityId",
                                isCatchAll: false,
                                isOptional: false,
                                defaultValue: null,
                                constraints: new EquatableArray<string>(["int"])
                            )
                        ]),
                        Errors: []
                    )
                )
            ])
        );

        var output = RouteLinkWriter.GenerateLink(options, linkContext);

        await
            Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }
}

using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Parsers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class AccoladesParserTests
{
    [Fact]
    public void Parse_Empty_ReturnsEmptyLog()
    {
        var model = AccoladesParser.Parse("""{ "CompletedAccolades": [] }""");

        model.CompletedAccolades.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Missing_CompletedAccolades_DefaultsEmpty()
    {
        var model = AccoladesParser.Parse("{}");

        model.CompletedAccolades.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Malformed_Throws()
    {
        var act = () => AccoladesParser.Parse("not json");

        act.Should().Throw<JsonException>();
    }
}

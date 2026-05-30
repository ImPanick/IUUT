using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Parsers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class BestiaryParserTests
{
    [Fact]
    public void Parse_Empty_ReturnsEmptySections()
    {
        var model = BestiaryParser.Parse("""{ "BestiaryTracking": [], "FishTracking": [] }""");

        model.BestiaryTracking.Should().BeEmpty();
        model.FishTracking.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MissingFishTracking_DefaultsEmpty()
    {
        var model = BestiaryParser.Parse("""{ "BestiaryTracking": [] }""");

        model.FishTracking.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Malformed_Throws()
    {
        var act = () => BestiaryParser.Parse("[1,2,3]");

        act.Should().Throw<JsonException>();
    }
}

using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Parsers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class ProfileParserTests
{
    [Fact]
    public void Parse_BasicProfile_ReadsKnownFields()
    {
        const string json = """
            {
                "UserID": "00000000000000000",
                "MetaResources": [ { "MetaRow": "Credits", "Count": 5840 } ],
                "UnlockedFlags": [ 5, 26, 1 ],
                "Talents": [ { "RowName": "Workshop_Envirosuit", "Rank": 1 } ],
                "NextChrSlot": 4,
                "DataVersion": 4
            }
            """;

        var profile = ProfileParser.Parse(json);

        profile.UserId.Should().Be("00000000000000000");
        profile.MetaResources.Should().ContainSingle(m => m.MetaRow == "Credits" && m.Count == 5840);
        profile.UnlockedFlags.Should().Equal(5, 26, 1);
        profile.Talents.Should().ContainSingle(t => t.RowName == "Workshop_Envirosuit" && t.Rank == 1);
        profile.NextChrSlot.Should().Be(4);
        profile.DataVersion.Should().Be(4);
    }

    [Fact]
    public void Parse_MapsUserIdFromUserIDKey()
    {
        var profile = ProfileParser.Parse("""{ "UserID": "00000000000000001" }""");

        profile.UserId.Should().Be("00000000000000001");
    }

    [Fact]
    public void Parse_Minimal_DefaultsEmptyCollections()
    {
        var profile = ProfileParser.Parse("{}");

        profile.UserId.Should().BeEmpty();
        profile.MetaResources.Should().BeEmpty();
        profile.UnlockedFlags.Should().BeEmpty();
        profile.Talents.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithCrlfLineEndings_Succeeds()
    {
        var json = "{\r\n\t\"UserID\": \"00000000000000000\",\r\n\t\"DataVersion\": 4\r\n}";

        var profile = ProfileParser.Parse(json);

        profile.UserId.Should().Be("00000000000000000");
        profile.DataVersion.Should().Be(4);
    }

    [Fact]
    public void Parse_Malformed_Throws()
    {
        var act = () => ProfileParser.Parse("this is not json");

        act.Should().Throw<JsonException>();
    }
}

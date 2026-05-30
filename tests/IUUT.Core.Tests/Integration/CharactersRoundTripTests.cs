using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public class CharactersRoundTripTests
{
    private static string Fixture() => Fixtures.ReadText("characters", "characters-basic.json");

    [Fact]
    public void Parse_Fixture_ReadsThreeCharacters()
    {
        var roster = CharactersParser.Parse(Fixture());

        roster.Should().HaveCount(3);
        roster.Select(c => c.ChrSlot).Should().Equal(1, 2, 3);
        roster.Select(c => c.CharacterName).Should().Equal("Char1", "Char2", "Char3");
    }

    [Fact]
    public void Parse_Fixture_ReadsCorrectedCosmeticBlock()
    {
        var c = CharactersParser.Parse(Fixture())[0];

        // The corrected cosmetic block is all integer indices (+ IsMale bool).
        c.Cosmetic.Customization_Head.Should().Be(12);
        c.Cosmetic.Customization_HairColor.Should().Be(3);
        c.Cosmetic.Customization_SkinTone.Should().Be(2);
        c.Cosmetic.Customization_EyeColor.Should().Be(5);
        c.Cosmetic.IsMale.Should().BeTrue();
        c.TimeLastPlayed.Should().Be(1700000000);
    }

    [Fact]
    public void RoundTrip_Fixture_PreservesFieldsAndUnknownMember()
    {
        var roster = CharactersParser.Parse(Fixture());

        var reserialized = CharactersSerializer.Serialize(roster);
        var roster2 = CharactersParser.Parse(reserialized);

        roster2.Should().HaveCount(3);
        roster2[1].XP.Should().Be(roster[1].XP);
        roster2[1].Talents.Should().HaveSameCount(roster[1].Talents);
        // Char3's unknown member survives the round-trip (CONSTITUTION VI).
        reserialized.Should().Contain("ExperimentalCharField");
        roster2[2].AdditionalData.Should().ContainKey("ExperimentalCharField");
    }

    [Fact]
    public void Serialize_ProducesNestedStringifiedShape()
    {
        var roster = CharactersParser.Parse(Fixture());

        var json = CharactersSerializer.Serialize(roster);

        // Snapshot of the container shape (DoD §3): single key, array of *stringified* objects.
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.EnumerateObject().Should().ContainSingle();
        var array = doc.RootElement.GetProperty("Characters.json");
        array.ValueKind.Should().Be(JsonValueKind.Array);
        array.GetArrayLength().Should().Be(3);
        foreach (var element in array.EnumerateArray())
        {
            element.ValueKind.Should().Be(JsonValueKind.String, "each character must be a stringified object");
            using var inner = JsonDocument.Parse(element.GetString()!);
            inner.RootElement.TryGetProperty("CharacterName", out _).Should().BeTrue();
        }
    }
}

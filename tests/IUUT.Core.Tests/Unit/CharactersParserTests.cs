using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class CharactersParserTests
{
    // Builds the nested-stringified container without hand-escaping: STJ escapes the
    // inner string for us when a Dictionary<string,string[]> is serialized.
    private static string Wrap(params string[] innerCharacterJson) =>
        JsonSerializer.Serialize(new Dictionary<string, string[]> { ["Characters.json"] = innerCharacterJson });

    [Fact]
    public void Parse_ReadsCoreFields()
    {
        var json = Wrap("""
            {"CharacterName":"Char1","ChrSlot":2,"XP":80000000,"XP_Debt":7,
             "IsDead":false,"TimeLastPlayed":1700000000,
             "Cosmetic":{"Customization_Head":12,"IsMale":true,"Customization_EyeColor":5},
             "Talents":[{"RowName":"Genetics_Twins","Rank":4}]}
            """);

        var roster = CharactersParser.Parse(json);

        roster.Should().ContainSingle();
        var c = roster[0];
        c.CharacterName.Should().Be("Char1");
        c.ChrSlot.Should().Be(2);
        c.XP.Should().Be(80000000);
        c.XP_Debt.Should().Be(7);
        c.TimeLastPlayed.Should().Be(1700000000);
        c.Cosmetic.Customization_Head.Should().Be(12);
        c.Cosmetic.IsMale.Should().BeTrue();
        c.Cosmetic.Customization_EyeColor.Should().Be(5);
        c.Talents.Should().ContainSingle(t => t.RowName == "Genetics_Twins" && t.Rank == 4);
    }

    [Fact]
    public void Parse_EmptyRoster_ReturnsEmptyList()
    {
        CharactersParser.Parse("""{ "Characters.json": [] }""").Should().BeEmpty();
    }

    [Fact]
    public void Parse_PreservesUnknownCharacterMembers()
    {
        var json = Wrap("""{"CharacterName":"Char1","ChrSlot":1,"FutureCharField":{"x":1}}""");

        var roster = CharactersParser.Parse(json);

        roster[0].AdditionalData.Should().ContainKey("FutureCharField");

        // Round-trip must reproduce the unknown member (CONSTITUTION VI).
        var reserialized = CharactersSerializer.Serialize(roster);
        reserialized.Should().Contain("FutureCharField");
    }

    [Fact]
    public void Parse_PreservesUnknownCosmeticMembers()
    {
        var json = Wrap("""{"CharacterName":"Char1","Cosmetic":{"Customization_Head":3,"Customization_FutureKnob":9}}""");

        var roster = CharactersParser.Parse(json);

        roster[0].Cosmetic.AdditionalData.Should().ContainKey("Customization_FutureKnob");
        CharactersSerializer.Serialize(roster).Should().Contain("Customization_FutureKnob");
    }
}

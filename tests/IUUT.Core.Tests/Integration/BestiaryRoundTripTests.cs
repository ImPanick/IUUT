using FluentAssertions;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public class BestiaryRoundTripTests
{
    private static string Fixture() => Fixtures.ReadText("bestiary", "bestiary-basic.json");

    [Fact]
    public void Parse_Fixture_ReadsBestiaryAndFishTracking()
    {
        var model = BestiaryParser.Parse(Fixture());

        model.BestiaryTracking.Should().HaveCount(2);
        model.BestiaryTracking[0].BestiaryGroup.RowName.Should().Be("Forest_Wolf");
        model.BestiaryTracking[0].NumPoints.Should().Be(1046);

        model.FishTracking.Should().ContainSingle();
        var fish = model.FishTracking[0];
        fish.FishRow.RowName.Should().Be("Fish_Example");
        fish.MaxQuality.Should().Be(3);
        fish.MaxWeight.Should().Be(1200);
        fish.CaughtCount.Should().Be(7);
    }

    [Fact]
    public void RoundTrip_Fixture_PreservesBothSections()
    {
        var model = BestiaryParser.Parse(Fixture());

        var model2 = BestiaryParser.Parse(BestiarySerializer.Serialize(model));

        model2.BestiaryTracking.Should().HaveSameCount(model.BestiaryTracking);
        model2.FishTracking.Should().HaveSameCount(model.FishTracking);
        model2.BestiaryTracking[1].NumPoints.Should().Be(model.BestiaryTracking[1].NumPoints);
    }
}

using FluentAssertions;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public class AccoladesRoundTripTests
{
    private static string Fixture() => Fixtures.ReadText("accolades", "accolades-basic.json");

    [Fact]
    public void Parse_Fixture_ReadsCompletedAccolades()
    {
        var model = AccoladesParser.Parse(Fixture());

        model.CompletedAccolades.Should().HaveCount(2);
        model.CompletedAccolades[0].Accolade.RowName.Should().Be("TutorialCompleted");
        model.CompletedAccolades[0].Accolade.DataTableName.Should().Be("D_Accolades");
        model.CompletedAccolades[0].TimeCompleted.Should().Be("2020.01.01-00.00.00");
    }

    [Fact]
    public void RoundTrip_Fixture_PreservesTrackerObjects()
    {
        var model = AccoladesParser.Parse(Fixture());

        // PlayerTrackers / PlayerTaskListTrackers are not modelled but must survive (CONSTITUTION VI).
        model.AdditionalData.Should().ContainKey("PlayerTrackers");
        model.AdditionalData.Should().ContainKey("PlayerTaskListTrackers");

        var reserialized = AccoladesSerializer.Serialize(model);
        reserialized.Should().Contain("PlayerTrackers");
        reserialized.Should().Contain("PlayerTaskListTrackers");
        reserialized.Should().Contain("ExampleCounter");

        var model2 = AccoladesParser.Parse(reserialized);
        model2.CompletedAccolades.Should().HaveSameCount(model.CompletedAccolades);
    }
}

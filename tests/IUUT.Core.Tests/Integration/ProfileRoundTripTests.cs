using FluentAssertions;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public class ProfileRoundTripTests
{
    [Fact]
    public void Parse_BasicFixture_ReadsAllKnownFields()
    {
        var json = Fixtures.ReadText("profiles", "profile-basic.json");

        var profile = ProfileParser.Parse(json);

        profile.UserId.Should().Be("00000000000000000");
        profile.MetaResources.Should().HaveCount(7);
        profile.MetaResources.Should().Contain(m => m.MetaRow == "Exotic_Uranium" && m.Count == 25);
        profile.UnlockedFlags.Should().HaveCount(12);
        profile.Talents.Should().HaveCount(2);
        profile.NextChrSlot.Should().Be(4);
        profile.DataVersion.Should().Be(4);
    }

    [Fact]
    public void RoundTrip_BasicFixture_PreservesKnownFields()
    {
        var json = Fixtures.ReadText("profiles", "profile-basic.json");
        var model = ProfileParser.Parse(json);

        var reserialized = ProfileSerializer.Serialize(model);
        var model2 = ProfileParser.Parse(reserialized);

        model2.UserId.Should().Be(model.UserId);
        model2.MetaResources.Should().HaveSameCount(model.MetaResources);
        model2.UnlockedFlags.Should().Equal(model.UnlockedFlags);
        model2.Talents.Should().HaveSameCount(model.Talents);
        model2.NextChrSlot.Should().Be(model.NextChrSlot);
        model2.DataVersion.Should().Be(model.DataVersion);
    }

    [Fact]
    public void RoundTrip_UnknownsFixture_PreservesUnknownMembers()
    {
        var json = Fixtures.ReadText("profiles", "profile-with-unknowns.json");
        var model = ProfileParser.Parse(json);

        // Unknown VALUES of known fields are normal entries.
        model.MetaResources.Should().Contain(m => m.MetaRow == "Exotic_Yellow" && m.Count == 7);
        model.Talents.Should().Contain(t => t.RowName == "Workshop_Future_Widget");
        model.DataVersion.Should().Be(5);

        // Unknown TOP-LEVEL members ride through extension data (CONSTITUTION VI).
        model.AdditionalData.Should().ContainKey("FutureAccountField");

        var reserialized = ProfileSerializer.Serialize(model);

        reserialized.Should().Contain("Exotic_Yellow");
        reserialized.Should().Contain("Workshop_Future_Widget");
        reserialized.Should().Contain("FutureAccountField");
        reserialized.Should().Contain("SomethingNew");
    }
}

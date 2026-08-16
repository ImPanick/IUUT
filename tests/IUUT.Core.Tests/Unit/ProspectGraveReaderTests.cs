using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Gates gravestone detection. Its whole job is telling a body apart from the furniture: a world
/// can hold hundreds of storage slots, and "your body is here, holding this" is the difference
/// between a usable rescue and a haystack.
/// </summary>
public class ProspectGraveReaderTests
{
    private static byte[] Deployable(string row, int guid, float x, float y, float z, int items) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.DeployableRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.NameProp("StaticItemDataRowName", row),
                UeFixtureBuilder.IntProp("IcarusActorGUID", guid),
                UeFixtureBuilder.ActorTransform(x, y, z),
                UeFixtureBuilder.StructArrayProp("Slots", "ItemSlot",
                    [.. Enumerable.Range(0, items).Select(i => UeFixtureBuilder.IntProp("Location", i))])));

    private static byte[] World() =>
        UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob",
        [
            Deployable("Storage_Plastic_Crate", 1, 0f, 0f, 0f, items: 12),
            Deployable(ProspectGraveReader.MissingRow, 2, 500_00f, -250_00f, 100_00f, items: 41),
            Deployable("Crafting_Bench", 3, 10f, 10f, 10f, items: 6),
            Deployable(ProspectGraveReader.DownedRow, 4, 0f, 100_00f, 0f, items: 0),
        ]);

    [Fact]
    public void Read_PicksGravesOutOfAWorldFullOfStorage()
    {
        var graves = new ProspectGraveReader().Read(World());

        graves.Should().HaveCount(2, "crates and benches are not bodies");
        graves.Select(g => g.Kind).Should().Contain(GraveKind.MissingInAction).And.Contain(GraveKind.DownedBody);
    }

    [Fact]
    public void Read_ReportsWhereTheBodyIsAndWhatItHolds()
    {
        var mia = new ProspectGraveReader().Read(World()).Single(g => g.Kind == GraveKind.MissingInAction);

        mia.ActorGuid.Should().Be(2);
        mia.ItemSlots.Should().Be(41);
        mia.HasItems.Should().BeTrue();
        mia.Placement!.Metres.Should().Be((500, -250, 100), "you need to know where to go, or where to move it to");
        mia.Label.Should().Be("missing-in-action marker");
    }

    [Fact]
    public void AnEmptyGrave_IsFoundButFlaggedAsNotWorthRecovering()
    {
        var downed = new ProspectGraveReader().Read(World()).Single(g => g.Kind == GraveKind.DownedBody);

        downed.HasItems.Should().BeFalse();
        downed.Label.Should().Be("downed body");
    }

    [Fact]
    public void Classify_OnlyMatchesTheTwoRealGravestoneRows()
    {
        ProspectGraveReader.Classify(ProspectGraveReader.MissingRow).Should().Be(GraveKind.MissingInAction);
        ProspectGraveReader.Classify(ProspectGraveReader.DownedRow).Should().Be(GraveKind.DownedBody);
        ProspectGraveReader.Classify("Storage_Plastic_Crate").Should().BeNull();
        ProspectGraveReader.Classify(null).Should().BeNull();
    }

    [Fact]
    public void GravesAreOrdinaryDeployableStorage_SoReturnToStashAlreadySweepsThem()
    {
        // The rescue path depends on this: a grave classifies as player-owned storage, which is
        // why its contents come home with everything else rather than needing a separate write.
        SlotOwner.Classify("/Script/Icarus.DeployableRecorderComponent")
            .Should().Be(SlotOwnerKind.DeployedStorage);
        SlotOwner.IsPlayerOwned("/Script/Icarus.DeployableRecorderComponent").Should().BeTrue();
    }
}

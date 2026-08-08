using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the read-only homestead survey: it must pick out what the PLAYER built (deployables,
/// beds) and ignore the world (resource deposits, creatures), and it must surface the cross-actor
/// links a future move would have to reconcile — foundation anchors, tame whitelists, and the
/// actor-id space.
/// </summary>
public class ProspectHomesteadReaderTests
{
    private static byte[] Actor(string componentClass, params byte[][] binaryProps) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", componentClass),
            UeFixtureBuilder.ByteStreamProp("BinaryData", binaryProps));

    private static byte[] World() =>
        UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob",
        [
            // A crate anchored to a foundation, with a tame whitelist.
            Actor("/Script/Icarus.DeployableRecorderComponent",
                UeFixtureBuilder.NameProp("StaticItemDataRowName", "Wooden_Crate"),
                UeFixtureBuilder.StructProp("DeployableRecord", "DeployableRecord",
                    UeFixtureBuilder.IntProp("FoundationActorIcarusUID", 4242)),
                UeFixtureBuilder.StructProp("TameInteractableRecord", "TameInteractableRecord",
                    UeFixtureBuilder.StructArrayProp("WhitelistedActors", "IntEntry",
                    [
                        UeFixtureBuilder.IntProp("Value", 7),
                        UeFixtureBuilder.IntProp("Value", 9),
                    ])),
                UeFixtureBuilder.IntProp("IcarusActorGUID", 1001)),

            // A free-standing bench.
            Actor("/Script/Icarus.DeployableRecorderComponent",
                UeFixtureBuilder.NameProp("StaticItemDataRowName", "Crafting_Bench"),
                UeFixtureBuilder.StructProp("DeployableRecord", "DeployableRecord",
                    UeFixtureBuilder.IntProp("FoundationActorIcarusUID", -1)),
                UeFixtureBuilder.IntProp("IcarusActorGUID", 1002)),

            // A bed counts as player-built. It records no StaticItemDataRowName, so it must
            // fall back to a readable label rather than showing as blank.
            Actor("/Script/Icarus.BedRecorderComponent",
                UeFixtureBuilder.IntProp("IcarusActorGUID", 1003)),

            // World actors that must NOT be counted as the player's base.
            Actor("/Script/Icarus.ResourceDepositRecorderComponent",
                UeFixtureBuilder.NameProp("StaticItemDataRowName", "Ore_Iron"),
                UeFixtureBuilder.IntProp("IcarusActorGUID", 5000),
                UeFixtureBuilder.StructProp("FLODComponentData", "FLODActorComponentSaveData",
                    UeFixtureBuilder.NameProp("TileName", "HeightMap_x1_y2"))),
            Actor("/Script/Icarus.CaveAIRecorderComponent",
                UeFixtureBuilder.IntProp("IcarusActorGUID", 5001)),
        ]);

    [Fact]
    public void Read_FindsOnlyPlayerBuiltStructures()
    {
        var survey = new ProspectHomesteadReader().Read(World());

        survey.TotalActors.Should().Be(5);
        survey.Structures.Should().HaveCount(3, "resource deposits and cave AI are world, not base");
        survey.ByKind.Select(k => k.RowName).Should().BeEquivalentTo(
            ["Wooden_Crate", "Crafting_Bench", "Bed"],
            "a recorder with no item row falls back to its kind, never a blank label");
    }

    [Fact]
    public void Read_SurfacesTheCrossActorLinksAMoveWouldHaveToReconcile()
    {
        var survey = new ProspectHomesteadReader().Read(World());

        var crate = survey.Structures.Single(s => s.RowName == "Wooden_Crate");
        crate.HasFoundation.Should().BeTrue("it is anchored to actor 4242");
        crate.FoundationUid.Should().Be(4242);
        crate.WhitelistedActorCount.Should().Be(2);

        survey.Structures.Single(s => s.RowName == "Crafting_Bench").HasFoundation
            .Should().BeFalse("-1 means free-standing");

        survey.FoundationLinked.Should().Be(1);
        survey.WhitelistLinked.Should().Be(1);

        // The id space a destination world would have to avoid colliding with.
        survey.DistinctActorGuids.Should().Be(5);
        survey.MaxActorGuid.Should().Be(5001);
        survey.TileNames.Should().BeEquivalentTo(["HeightMap_x1_y2"]);
    }

    [Fact]
    public void Read_NoRecorders_IsEmptyNotAnError()
    {
        var survey = new ProspectHomesteadReader().Read(UeFixtureBuilder.StrProp("Unrelated", "x"));

        survey.Structures.Should().BeEmpty();
        survey.TotalActors.Should().Be(0);
    }
}

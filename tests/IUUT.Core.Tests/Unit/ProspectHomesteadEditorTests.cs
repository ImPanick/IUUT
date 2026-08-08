using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Gates base relocation — the first homestead write. It must move exactly the selected build,
/// leave every other actor byte-identical, preserve the blob's length (in-place writes only),
/// and keep foundation links intact (actor ids are never touched).
/// </summary>
public class ProspectHomesteadEditorTests
{
    private static byte[] Deployable(string row, int guid, float x, float y, float z, int foundationUid = -1) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.DeployableRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.NameProp("StaticItemDataRowName", row),
                UeFixtureBuilder.StructProp("DeployableRecord", "DeployableRecord",
                    UeFixtureBuilder.IntProp("FoundationActorIcarusUID", foundationUid)),
                UeFixtureBuilder.IntProp("IcarusActorGUID", guid),
                UeFixtureBuilder.ActorTransform(x, y, z)));

    // Two builds 500 m apart, plus a world actor that must never move.
    private static byte[] World() =>
        UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob",
        [
            Deployable("Wall", 100, 0f, 0f, 0f),
            Deployable("Bench", 101, 1000f, 0f, 0f, foundationUid: 100),   // 10 m away — same build
            Deployable("Beacon", 200, 5_000_00f, 0f, 0f),                  // 5 km away — its own build
            UeFixtureBuilder.Concat(
                UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.ResourceDepositRecorderComponent"),
                UeFixtureBuilder.ByteStreamProp(
                    "BinaryData",
                    UeFixtureBuilder.IntProp("IcarusActorGUID", 900),
                    UeFixtureBuilder.ActorTransform(7f, 8f, 9f))),
        ]);

    [Fact]
    public void Clusters_SeparateNearbyBuildsFromDistantOnes()
    {
        var survey = new ProspectHomesteadReader().Read(World());

        var clusters = survey.Clusters(radiusMetres: 60);

        clusters.Should().HaveCount(2);
        clusters[0].Count.Should().Be(2, "the wall and bench are 10 m apart");
        clusters[0].TopKinds.Should().Contain(k => k.Contains("Wall") || k.Contains("Bench"));
        clusters[1].Count.Should().Be(1, "the beacon is 5 km out on its own");
    }

    [Fact]
    public void Clusters_ExcludeTheWorldContainerRegistry()
    {
        var data = UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob",
        [
            Deployable("Wall", 100, 0f, 0f, 0f),
            UeFixtureBuilder.Concat(
                UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.IcarusContainerManagerRecorderComponent"),
                UeFixtureBuilder.ByteStreamProp(
                    "BinaryData",
                    UeFixtureBuilder.IntProp("IcarusActorGUID", 1),
                    UeFixtureBuilder.ActorTransform(0f, 0f, 0f))),
        ]);

        var survey = new ProspectHomesteadReader().Read(data);

        survey.Structures.Should().HaveCount(2, "the registry holds player items, so item rescue still sees it");
        survey.Clusters().Should().ContainSingle("but it is a singleton at the origin, not a build you can move")
            .Which.Count.Should().Be(1);
    }

    [Fact]
    public void Move_ShiftsOnlyTheSelectedBuild_AndIsSizePreserving()
    {
        var data = World();
        var original = (byte[])data.Clone();
        var survey = new ProspectHomesteadReader().Read(data);
        var build = survey.Clusters()[0];

        var result = ProspectHomesteadEditor.Move(data, build.Structures.Select(s => s.ActorGuid), 10, -20, 5);

        result.StructuresMoved.Should().Be(2);
        data.Length.Should().Be(original.Length, "translations are overwritten in place");

        // Exactly 2 structures x 3 floats = 6 four-byte fields may differ.
        var differing = Enumerable.Range(0, data.Length).Count(i => data[i] != original[i]);
        differing.Should().BeLessThanOrEqualTo(24, "only the selected build's translation bytes change");

        var after = new ProspectHomesteadReader().Read(data);
        var wall = after.Structures.Single(s => s.ActorGuid == 100).Placement!;
        wall.X.Should().Be(1000f, "10 m east of 0 in centimetres");
        wall.Y.Should().Be(-2000f);
        wall.Z.Should().Be(500f);

        var beacon = after.Structures.Single(s => s.ActorGuid == 200).Placement!;
        beacon.X.Should().Be(5_000_00f, "the distant build is untouched");

        // The world actor and the foundation link both survive.
        after.Structures.Single(s => s.ActorGuid == 101).FoundationUid.Should().Be(100);
        after.TotalActors.Should().Be(4);
    }

    [Fact]
    public void Move_ZeroOffsetOrUnknownActors_ChangeNothing()
    {
        var data = World();
        var original = (byte[])data.Clone();

        ProspectHomesteadEditor.Move(data, [100, 101], 0, 0, 0).Changed.Should().BeFalse();
        ProspectHomesteadEditor.Move(data, [], 10, 10, 10).Changed.Should().BeFalse();
        ProspectHomesteadEditor.Move(data, [12345], 10, 10, 10).StructuresMoved.Should().Be(0);

        data.Should().Equal(original);
    }
}

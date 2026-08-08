using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Gates the ground-height model. The behaviour that matters is not the number it returns but
/// whether it admits when it is guessing: every catastrophic miss measured against real saves
/// (up to 191 m) fell in the low-confidence buckets, so confidence is the safety mechanism.
/// </summary>
public class TerrainHeightFieldTests
{
    private static byte[] Deposit(int guid, float x, float y, float z) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.ResourceDepositRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.IntProp("IcarusActorGUID", guid),
                UeFixtureBuilder.ActorTransform(x, y, z)));

    private static byte[] Deployable(int guid, float x, float y, float z) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.DeployableRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.NameProp("StaticItemDataRowName", "Crate"),
                UeFixtureBuilder.IntProp("IcarusActorGUID", guid),
                UeFixtureBuilder.ActorTransform(x, y, z)));

    // A flat plain at 50 m: nine samples on a 10 m grid (positions in centimetres).
    private static byte[] FlatWorld(params byte[][] extra)
    {
        var actors = new List<byte[]>();
        var guid = 1;
        for (var i = -1; i <= 1; i++)
        {
            for (var j = -1; j <= 1; j++)
            {
                actors.Add(Deposit(guid++, i * 1000f, j * 1000f, 5000f));
            }
        }

        actors.AddRange(extra);
        return UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob", [.. actors]);
    }

    [Fact]
    public void EstimateAt_OnEvenGround_IsAccurateAndConfident()
    {
        var field = TerrainHeightField.FromBlob(FlatWorld());

        var estimate = field.EstimateAt(2, 3);

        estimate.Should().NotBeNull();
        estimate!.HeightMetres.Should().BeApproximately(50, 0.001, "every sample around it sits at 50 m");
        estimate.Confidence.Should().Be(TerrainHeightConfidence.High);
        estimate.NeighbourSpreadMetres.Should().BeApproximately(0, 0.001);
        estimate.Explanation.Should().Contain("even");
    }

    [Fact]
    public void EstimateAt_OnBrokenGround_DropsToLowConfidence()
    {
        // Same footprint, but the samples disagree wildly — a cliff or a cave mouth.
        var actors = new List<byte[]>();
        var guid = 1;
        for (var i = -1; i <= 1; i++)
        {
            for (var j = -1; j <= 1; j++)
            {
                actors.Add(Deposit(guid++, i * 1000f, j * 1000f, i < 0 ? 0f : 8000f));
            }
        }

        var field = TerrainHeightField.FromBlob(
            UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob", [.. actors]));

        var estimate = field.EstimateAt(0, 0);

        estimate.Should().NotBeNull();
        estimate!.Confidence.Should().Be(TerrainHeightConfidence.Low);
        estimate.NeighbourSpreadMetres.Should().BeGreaterThan(15);
        estimate.Explanation.Should().ContainAny("cliff", "slope", "cave");
    }

    [Fact]
    public void EstimateAt_FarFromAnySample_DropsToLowConfidence()
    {
        var field = TerrainHeightField.FromBlob(FlatWorld());

        var estimate = field.EstimateAt(500, 500);

        estimate.Should().NotBeNull();
        estimate!.Confidence.Should().Be(TerrainHeightConfidence.Low, "there is nothing within 60 m to measure against");
        estimate.NearestSampleMetres.Should().BeGreaterThan(60);
        estimate.Explanation.Should().Contain("nothing within");
    }

    [Fact]
    public void PlayerBuiltStructures_AreNotUsedAsGroundSamples()
    {
        // A crate on stilts 300 m up must not teach the field that the ground is 300 m up.
        var withCrate = TerrainHeightField.FromBlob(FlatWorld(Deployable(99, 0f, 0f, 30_000f)));

        withCrate.SampleCount.Should().Be(9, "only the nine deposits count as ground");
        withCrate.EstimateAt(0, 0)!.HeightMetres.Should().BeApproximately(50, 0.001);
    }

    [Fact]
    public void EstimateAt_WithTooFewSamples_ReturnsNull()
    {
        var field = TerrainHeightField.FromBlob(
            UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob",
                [Deposit(1, 0f, 0f, 100f), Deposit(2, 100f, 0f, 100f)]));

        field.SampleCount.Should().Be(2);
        field.EstimateAt(0, 0).Should().BeNull("two samples cannot describe a landscape");
    }

    [Fact]
    public void ActorsAtTheWorldOrigin_AreNotTreatedAsGround()
    {
        var field = TerrainHeightField.FromBlob(FlatWorld(Deposit(99, 0f, 0f, 0f)));

        field.SampleCount.Should().Be(9, "an actor at exactly (0,0,0) is an unplaced singleton");
    }
}

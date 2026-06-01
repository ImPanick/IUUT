using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Round-trip gate for the write-capable UE blob model. The same byte-identical reconstruction was
/// verified against real prospect worlds (incl. a 17 MB / 2768-actor save) in scratch; these synthetic
/// fixtures guard it in CI without shipping any real save data.
/// </summary>
public class UeBlobTests
{
    private static byte[] SampleWorld() =>
        UeFixtureBuilder.StructArrayProp(
            "StateRecorderBlobs",
            "StateRecorderBlob",
            new[]
            {
                UeFixtureBuilder.Concat(
                    UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.TestRecorderComponent"),
                    UeFixtureBuilder.ByteStreamProp(
                        "BinaryData",
                        UeFixtureBuilder.IntProp("Completions", 3),
                        UeFixtureBuilder.NameProp("HordeDTKey", "Forest_Horde"),
                        UeFixtureBuilder.StructProp(
                            "SavedInventories",
                            "InventorySaveData",
                            UeFixtureBuilder.StructArrayProp(
                                "Slots",
                                "InventorySlotSaveData",
                                new[]
                                {
                                    UeFixtureBuilder.NameProp("ItemStaticData", "Wood"),
                                    UeFixtureBuilder.NameProp("ItemStaticData", "Stone"),
                                })))),
            });

    [Fact]
    public void Serialize_ForceReconstruct_IsByteIdentical()
    {
        var world = SampleWorld();

        var round = UeBlob.Parse(world).Serialize(forceReconstruct: true);

        round.Should().Equal(world, "every header is rebuilt from parsed fields and must reproduce the input exactly");
    }

    [Fact]
    public void Serialize_NoEdits_IsByteIdentical()
    {
        var world = SampleWorld();

        var round = UeBlob.Parse(world).Serialize();

        round.Should().Equal(world);
    }

    [Fact]
    public void Parse_ExposesTheActorArrayAndNestedInventory()
    {
        var blob = UeBlob.Parse(SampleWorld());

        var actors = blob.Roots.Should().ContainSingle(r => r.Name == "StateRecorderBlobs").Subject;
        actors.Kind.Should().Be(UeNodeKind.StructArray);
        actors.Children.Should().ContainSingle("one actor element");

        // Drill into the recorder BinaryData nested stream → SavedInventories → Slots.
        var binaryData = FindByName(blob.Roots, "BinaryData");
        binaryData.Should().NotBeNull();
        binaryData!.Kind.Should().Be(UeNodeKind.ByteStream);

        var slots = FindByName(blob.Roots, "Slots");
        slots.Should().NotBeNull();
        slots!.Kind.Should().Be(UeNodeKind.StructArray);
        slots.Children.Should().HaveCount(2, "two filled slots");
    }

    [Fact]
    public void Serialize_RandomGarbage_DoesNotThrow_AndPreservesBytes()
    {
        var garbage = Enumerable.Range(0, 4096).Select(i => (byte)(i * 53 % 256)).ToArray();

        var round = UeBlob.Parse(garbage).Serialize(forceReconstruct: true);

        round.Should().Equal(garbage, "unparseable bytes fall through to the verbatim document tail");
    }

    private static UeNode? FindByName(IEnumerable<UeNode> nodes, string name)
    {
        foreach (var n in nodes)
        {
            if (string.Equals(n.Name, name, System.StringComparison.Ordinal))
            {
                return n;
            }

            var hit = FindByName(n.Children, name);
            if (hit is not null)
            {
                return hit;
            }
        }

        return null;
    }
}

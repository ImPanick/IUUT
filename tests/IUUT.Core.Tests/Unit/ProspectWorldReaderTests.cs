using FluentAssertions;
using IUUT.Core.Catalog;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the prospect world-blob reader (the in-prospect inventory spike) against synthetic UE
/// tagged-property streams built by <see cref="UeFixtureBuilder"/> — no real save data. Mirrors the
/// confirmed on-disk shape: <c>StateRecorderBlobs</c> → actor → <c>BinaryData</c> nested stream →
/// <c>SavedInventories</c> (<c>InventorySaveData</c>) → <c>Slots</c> (<c>InventorySlotSaveData</c>) →
/// <c>ItemStaticData</c>.
/// </summary>
public class ProspectWorldReaderTests
{
    private static CatalogTable ItemsCatalog() => new(
        "D_ItemsStatic",
        "test",
        new[]
        {
            new CatalogRow { RowName = "Wood", DisplayName = "Timber" }, // curated name to prove resolution
            new CatalogRow { RowName = "Stone" },                        // humanized fallback
        });

    private static byte[] WorldWithItems(params string[] itemRowNames)
    {
        var slots = UeFixtureBuilder.StructArrayProp(
            "Slots",
            "InventorySlotSaveData",
            itemRowNames.Select(r => UeFixtureBuilder.NameProp("ItemStaticData", r)).ToList());

        var inventory = UeFixtureBuilder.StructProp("SavedInventories", "InventorySaveData", slots);

        var actor = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.TestRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp("BinaryData", inventory));

        return UeFixtureBuilder.StructArrayProp(
            "StateRecorderBlobs",
            "StateRecorderBlob",
            new[] { actor });
    }

    [Fact]
    public void Read_FindsItemsInsideTheRecorderBinaryDataStream()
    {
        var world = WorldWithItems("Wood", "Stone", "Wood");

        var report = new ProspectWorldReader(ItemsCatalog()).Read(world);

        report.ActorRecordCount.Should().Be(1);
        report.InventoryCount.Should().Be(1, "one InventorySaveData struct");
        report.SlotCount.Should().Be(3, "three InventorySlotSaveData elements");
        report.Items.Should().HaveCount(3);
        report.ItemCounts.Should().Contain(new KeyValuePair<string, int>("Wood", 2));
        report.ItemCounts.Should().Contain(new KeyValuePair<string, int>("Stone", 1));
    }

    [Fact]
    public void Read_ResolvesFriendlyNames_CuratedAndHumanizedFallback()
    {
        var report = new ProspectWorldReader(ItemsCatalog()).Read(WorldWithItems("Wood", "Stone"));

        report.Items.Should().ContainSingle(i => i.RowName == "Wood").Which.DisplayName.Should().Be("Timber");
        report.Items.Should().ContainSingle(i => i.RowName == "Stone").Which.DisplayName.Should().Be("Stone");
    }

    [Fact]
    public void Read_EmptyWorld_ReturnsNoItems_AndDoesNotThrow()
    {
        var emptyActor = UeFixtureBuilder.StructArrayProp(
            "StateRecorderBlobs",
            "StateRecorderBlob",
            new[] { UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.EmptyComponent") });

        var report = new ProspectWorldReader(ItemsCatalog()).Read(emptyActor);

        report.ActorRecordCount.Should().Be(1);
        report.Items.Should().BeEmpty();
        report.SlotCount.Should().Be(0);
    }

    [Fact]
    public void Read_GarbageBytes_DoesNotThrow_AndReportsNothing()
    {
        var garbage = Enumerable.Range(0, 5000).Select(i => (byte)(i * 37 % 256)).ToArray();

        var report = new ProspectWorldReader(ItemsCatalog()).Read(garbage);

        report.Items.Should().BeEmpty();
    }
}

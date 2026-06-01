using FluentAssertions;
using IUUT.Core.Catalog;
using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the mutating prospect world editor (remove / duplicate / retype) against synthetic UE
/// buffers. The same edits were validated end-to-end on real prospects in scratch (edit → re-serialize
/// → recompress → VerifyHash passes; the edited stream force-reconstructs to itself, proving every
/// recomputed length prefix is consistent). These fixtures guard that in CI with no real save data.
/// </summary>
public class ProspectWorldEditorTests
{
    private static CatalogTable Items() => new("D_ItemsStatic", "test", Array.Empty<CatalogRow>());

    private static byte[] WorldWith(params string[] items) =>
        UeFixtureBuilder.StructArrayProp(
            "StateRecorderBlobs",
            "StateRecorderBlob",
            new[]
            {
                UeFixtureBuilder.Concat(
                    UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.TestRecorderComponent"),
                    UeFixtureBuilder.ByteStreamProp(
                        "BinaryData",
                        UeFixtureBuilder.StructProp(
                            "SavedInventories",
                            "InventorySaveData",
                            UeFixtureBuilder.StructArrayProp(
                                "Slots",
                                "InventorySlotSaveData",
                                items.Select(i => UeFixtureBuilder.Concat(
                                    UeFixtureBuilder.NameProp("ItemStaticData", i),
                                    UeFixtureBuilder.StrProp("ItemGuid", ""),
                                    UeFixtureBuilder.IntProp("ItemOwnerLookupId", -1))).ToList())))),
            });

    private static IReadOnlyDictionary<string, int> CountsOf(byte[] world) =>
        new ProspectWorldReader(Items()).Read(world).ItemCounts;

    private static bool ReparsesStably(byte[] world) =>
        UeBlob.Parse(world).Serialize(forceReconstruct: true).AsSpan().SequenceEqual(world);

    [Fact]
    public void RemoveSlot_DropsTheItem_AndProducesAConsistentStream()
    {
        var editor = new ProspectWorldEditor(UeBlob.Parse(WorldWith("Wood", "Stone", "Wood")));
        editor.RemoveSlot(editor.FindItemSlots().First(s => s.RowName == "Wood"));

        var edited = editor.Serialize();

        var counts = CountsOf(edited);
        counts.GetValueOrDefault("Wood").Should().Be(1, "one of the two Wood slots was removed");
        counts.GetValueOrDefault("Stone").Should().Be(1);
        ReparsesStably(edited).Should().BeTrue("all length prefixes must be recomputed consistently");
    }

    [Fact]
    public void DuplicateSlot_AddsACopy_AndProducesAConsistentStream()
    {
        var editor = new ProspectWorldEditor(UeBlob.Parse(WorldWith("Wood", "Stone")));
        editor.DuplicateSlot(editor.FindItemSlots().First(s => s.RowName == "Stone"));

        var edited = editor.Serialize();

        CountsOf(edited).GetValueOrDefault("Stone").Should().Be(2);
        ReparsesStably(edited).Should().BeTrue();
    }

    [Fact]
    public void RetypeSlot_ChangesItemIdentity()
    {
        var editor = new ProspectWorldEditor(UeBlob.Parse(WorldWith("Wood", "Stone")));
        editor.RetypeSlot(editor.FindItemSlots().First(s => s.RowName == "Wood"), "Charcoal");

        var counts = CountsOf(editor.Serialize());

        counts.GetValueOrDefault("Wood").Should().Be(0);
        counts.GetValueOrDefault("Charcoal").Should().Be(1);
        counts.GetValueOrDefault("Stone").Should().Be(1);
    }

    [Fact]
    public void DuplicateWithNewItem_AddsTheNewItem()
    {
        var editor = new ProspectWorldEditor(UeBlob.Parse(WorldWith("Wood")));
        editor.DuplicateSlot(editor.FindItemSlots()[0], newItemRowName: "Refined_Metal");

        var counts = CountsOf(editor.Serialize());

        counts.GetValueOrDefault("Wood").Should().Be(1, "the original is untouched");
        counts.GetValueOrDefault("Refined_Metal").Should().Be(1, "the clone got the new identity");
    }

    [Fact]
    public void FindItemSlots_ReadsStackAndDurability_FromDynamicData()
    {
        var world = UeFixtureBuilder.WorldWithSlots(new[]
        {
            UeFixtureBuilder.InventorySlot("Wood", (ProspectWorldEditor.StackIndex, 100), (ProspectWorldEditor.DurabilityIndex, 150000)),
            UeFixtureBuilder.InventorySlot("Stone", (ProspectWorldEditor.StackIndex, 42)),
        });

        var slots = new ProspectWorldEditor(UeBlob.Parse(world)).FindItemSlots();

        var wood = slots.Single(s => s.RowName == "Wood");
        wood.Stack.Should().Be(100);
        wood.Durability.Should().Be(150000);
        var stone = slots.Single(s => s.RowName == "Stone");
        stone.Stack.Should().Be(42);
        stone.Durability.Should().BeNull("no Index-9 entry on this slot");
    }

    [Fact]
    public void SetStack_ChangesTheStackInPlace_AndStreamStaysConsistent()
    {
        var editor = new ProspectWorldEditor(UeBlob.Parse(
            UeFixtureBuilder.WorldWithSlots(new[] { UeFixtureBuilder.InventorySlot("Wood", (ProspectWorldEditor.StackIndex, 10)) })));
        editor.SetStack(editor.FindItemSlots()[0], 77).Should().BeTrue();

        var edited = editor.Serialize();

        ReparsesStably(edited).Should().BeTrue();
        new ProspectWorldEditor(UeBlob.Parse(edited)).FindItemSlots()[0].Stack.Should().Be(77);
    }

    [Fact]
    public void FindItemSlots_ExposesOwnerComponentClass_ForScoping()
    {
        var world = UeFixtureBuilder.WorldWithSlots(
            "/Script/Icarus.PlayerStateRecorderComponent",
            new[] { UeFixtureBuilder.InventorySlot("Wood", (ProspectWorldEditor.StackIndex, 1)) });

        var slot = new ProspectWorldEditor(UeBlob.Parse(world)).FindItemSlots()[0];

        slot.OwnerComponentClass.Should().Be("/Script/Icarus.PlayerStateRecorderComponent");
        SlotOwner.Classify(slot.OwnerComponentClass).Should().Be(SlotOwnerKind.PlayerCarried);
    }

    [Fact]
    public void SetDurability_RepairsTheItem()
    {
        var editor = new ProspectWorldEditor(UeBlob.Parse(UeFixtureBuilder.WorldWithSlots(new[]
        {
            UeFixtureBuilder.InventorySlot("Crossbow", (ProspectWorldEditor.StackIndex, 1), (ProspectWorldEditor.DurabilityIndex, 5000)),
        })));
        editor.SetDurability(editor.FindItemSlots()[0], 150000).Should().BeTrue();

        new ProspectWorldEditor(UeBlob.Parse(editor.Serialize())).FindItemSlots()[0].Durability.Should().Be(150000);
    }

    [Fact]
    public void EditedWorld_RewrappedViaCodec_PassesTheGameHashCheck()
    {
        var editor = new ProspectWorldEditor(UeBlob.Parse(WorldWith("Wood", "Stone", "Wood")));
        editor.DuplicateSlot(editor.FindItemSlots()[0]);
        var edited = editor.Serialize();

        var blob = new ProspectBlobModel { Key = "actors" };
        ProspectBlobCodec.SetUncompressed(blob, edited);

        ProspectBlobVerifier.VerifyHash(blob).Should().BeTrue("the re-stamped blob must pass the game's load-time hash");
        ProspectBlobCodec.Decompress(blob.BinaryBlob).Should().Equal(edited);
        blob.UncompressedLength.Should().Be(edited.Length);
    }
}

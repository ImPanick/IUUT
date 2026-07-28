using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Gates the in-prospect mount rename — the first length-changing string write beyond item
/// retyping: the renamed blob must re-read with the new name (longer AND shorter), sibling
/// item slots in the SAME recorder stream must survive the length fixup, and a MountName
/// string owned by any other component must never be a candidate.
/// </summary>
public class ProspectMountEditorTests
{
    private static byte[] MountWorld(string mountName)
    {
        var mountActor = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.IcarusMountCharacterRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.StrProp("MountName", mountName),
                UeFixtureBuilder.StructProp("SavedInventories", "InventorySaveData",
                    UeFixtureBuilder.StructArrayProp("Slots", "InventorySlotSaveData",
                        [UeFixtureBuilder.InventorySlot("Item_Saddlebag", (7, 12))]))));

        // A decoy: same property name, different owner — must be ignored.
        var decoy = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.SomeOtherRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp("BinaryData", UeFixtureBuilder.StrProp("MountName", "Decoy")));

        return UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob", [mountActor, decoy]);
    }

    [Theory]
    [InlineData("A Much Longer Mount Name Than Before")]
    [InlineData("X")]
    public void Rename_RoundTrips_AndKeepsSiblingSlotsIntact(string newName)
    {
        var editor = new ProspectMountEditor(UeBlob.Parse(MountWorld("Original")));
        var mounts = editor.FindMounts();
        mounts.Should().ContainSingle("the decoy's MountName is owned by another component")
            .Which.Name.Should().Be("Original");

        editor.Rename(mounts[0], newName);
        var rewritten = editor.Serialize();

        new ProspectMountReader().Read(rewritten)
            .Should().ContainSingle().Which.Name.Should().Be(newName);

        var slots = new ProspectWorldEditor(UeBlob.Parse(rewritten)).FindItemSlots();
        slots.Should().ContainSingle("the sibling slot must survive the length fixup");
        slots[0].RowName.Should().Be("Item_Saddlebag");
        slots[0].Stack.Should().Be(12);
    }
}

using FluentAssertions;
using IUUT.Core.Abstractions;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the "return trapped items → orbital stash" service against synthetic prospect worlds. The
/// same flow was validated on real prospects in scratch (99 slots / 6268 items returned, totals matched,
/// stacks coalesced to ≤100, and the re-stamped prospect passed VerifyHash).
/// </summary>
public class ProspectReturnServiceTests
{
    private static ProspectReturnService Service() =>
        new(new StashEditService(new SystemGuidProvider()));

    private static ProspectFileModel WrapWorld(byte[] world)
    {
        var blob = new ProspectBlobModel { Key = "actors" };
        ProspectBlobCodec.SetUncompressed(blob, world);
        return new ProspectFileModel { ProspectBlob = blob };
    }

    private static byte[] World(params byte[][] slots) => UeFixtureBuilder.WorldWithSlots(slots);

    private static int StackIdx => ProspectWorldEditor.StackIndex;

    [Fact]
    public void Preview_SumsStacksPerItem_AcrossSlots()
    {
        var prospect = WrapWorld(World(
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 100)),
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 50)),
            UeFixtureBuilder.InventorySlot("Stone", (StackIdx, 20))));

        var preview = Service().Preview(prospect);

        var wood = preview.Single(i => i.RowName == "Wood");
        wood.TotalQuantity.Should().Be(150, "two Wood slots of 100 + 50");
        wood.SlotCount.Should().Be(2);
        preview.Single(i => i.RowName == "Stone").TotalQuantity.Should().Be(20);
    }

    [Fact]
    public void Return_MovesEverythingToStash_CoalescedAndCapped_AndEmptiesTheProspect()
    {
        var prospect = WrapWorld(World(
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 100)),
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 100)),
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 50)))); // 250 total
        var stash = new MetaInventoryModel { InventoryId = "1" };

        var result = Service().Return(prospect, stash);

        result.TotalQuantity.Should().Be(250);
        result.SlotsRemoved.Should().Be(3);
        stash.Items.Sum(StashEditService.GetStack).Should().Be(250, "quantity is conserved");
        stash.Items.Should().HaveCount(3, "250 coalesced into 100 + 100 + 50");
        stash.Items.Max(StashEditService.GetStack).Should().BeLessThanOrEqualTo(StashEditService.MaxStack);
        ProspectBlobVerifier.VerifyHash(prospect.ProspectBlob).Should().BeTrue("the re-stamped prospect must pass the game's hash");
        SlotsLeftIn(prospect).Should().Be(0, "all items were removed from the prospect world");
    }

    [Fact]
    public void Return_WithRowNameFilter_OnlyReturnsTheSelectedItems()
    {
        var prospect = WrapWorld(World(
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 10)),
            UeFixtureBuilder.InventorySlot("Stone", (StackIdx, 20))));
        var stash = new MetaInventoryModel { InventoryId = "1" };

        Service().Return(prospect, stash, new HashSet<string> { "Wood" });

        stash.Items.Should().ContainSingle();
        ProspectBlobVerifier.VerifyHash(prospect.ProspectBlob).Should().BeTrue();
        SlotsLeftIn(prospect).Should().Be(1, "the unselected Stone slot stays in the prospect");
    }

    [Fact]
    public void ReturnPlayerOwned_ReturnsPlayerItems_ButLeavesMachineInventories()
    {
        var player = WrapWorld(UeFixtureBuilder.WorldWithSlots(
            "/Script/Icarus.PlayerStateRecorderComponent",
            new[] { UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 10)) }));
        var stash = new MetaInventoryModel { InventoryId = "1" };
        Service().ReturnPlayerOwned(player, stash);
        stash.Items.Sum(StashEditService.GetStack).Should().Be(10);
        SlotsLeftIn(player).Should().Be(0, "the player's carried items are returned");

        var machine = WrapWorld(UeFixtureBuilder.WorldWithSlots(
            "/Script/Icarus.DrillRecorderComponent",
            new[] { UeFixtureBuilder.InventorySlot("Ore", (StackIdx, 10)) }));
        var stash2 = new MetaInventoryModel { InventoryId = "1" };
        Service().ReturnPlayerOwned(machine, stash2);
        stash2.Items.Should().BeEmpty("a drill's inventory is not player-owned");
        SlotsLeftIn(machine).Should().Be(1, "the machine's items stay put");
    }

    private static int SlotsLeftIn(ProspectFileModel prospect) =>
        new ProspectWorldEditor(UeBlob.Parse(ProspectBlobCodec.Decompress(prospect.ProspectBlob.BinaryBlob)))
            .FindItemSlots().Count;
}

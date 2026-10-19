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
/// Gates "return trapped items to the orbital stash".
/// <para>
/// The previous version of these tests asserted that returning a prospect's Wood put Wood into the
/// stash — and called that a pass. The orbital stash holds workshop (<c>Item.Meta</c>) items only; the
/// game has never written anything else to it on any available save, and on real prospects more than
/// 99% of what is trapped is raw material. The tests now pin the opposite: an item the stash cannot
/// hold is never removed from the world, and an item that can is returned as the world held it.
/// </para>
/// </summary>
public class ProspectReturnServiceTests
{
    // Stand-in for the Item.Meta catalog: workshop rows are named Meta_* in the real game.
    private static bool IsWorkshop(string row) => row.StartsWith("Meta_", StringComparison.Ordinal);

    private static ProspectReturnService Service() =>
        new(new StashEditService(new SystemGuidProvider()), IsWorkshop);

    private static ProspectFileModel WrapWorld(byte[] world)
    {
        var blob = new ProspectBlobModel { Key = "actors" };
        ProspectBlobCodec.SetUncompressed(blob, world);
        return new ProspectFileModel { ProspectBlob = blob };
    }

    private static byte[] World(params byte[][] slots) => UeFixtureBuilder.WorldWithSlots(slots);

    private static int StackIdx => ProspectWorldEditor.StackIndex;

    private static int DurIdx => ProspectWorldEditor.DurabilityIndex;

    [Fact]
    public void Preview_SumsStacksPerItem_AndFlagsWhatTheStashCanHold()
    {
        var prospect = WrapWorld(World(
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 100)),
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 50)),
            UeFixtureBuilder.InventorySlot("Meta_Arrow", (StackIdx, 6))));

        var preview = Service().Preview(prospect);

        var wood = preview.Single(i => i.RowName == "Wood");
        wood.TotalQuantity.Should().Be(150);
        wood.SlotCount.Should().Be(2);
        wood.CanGoToStash.Should().BeFalse("raw materials cannot enter the orbital stash");
        preview.Single(i => i.RowName == "Meta_Arrow").CanGoToStash.Should().BeTrue();
        preview[0].RowName.Should().Be("Meta_Arrow", "what can come home is listed first");
    }

    [Fact]
    public void Return_NeverPutsAnItemTheStashCannotHoldIntoTheStash()
    {
        var prospect = WrapWorld(World(
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 100)),
            UeFixtureBuilder.InventorySlot("Charcoal", (StackIdx, 80))));
        var stash = new MetaInventoryModel { InventoryId = "1" };
        var before = prospect.ProspectBlob.BinaryBlob;

        var result = Service().Return(prospect, stash);

        stash.Items.Should().BeEmpty("the game never writes a non-Meta row to the stash");
        result.SlotsRemoved.Should().Be(0);
        result.LeftBehind.Sum(i => i.TotalQuantity).Should().Be(180, "the result says what stayed and how much");
        prospect.ProspectBlob.BinaryBlob.Should().Be(before, "nothing moved, so the world is not even re-encoded");
        SlotsLeftIn(prospect).Should().Be(2, "the items are exactly where they were");
    }

    [Fact]
    public void Return_MovesOnlyWorkshopItems_AndLeavesTheRestInTheWorld()
    {
        var prospect = WrapWorld(World(
            UeFixtureBuilder.InventorySlot("Meta_Arrow", (StackIdx, 6)),
            UeFixtureBuilder.InventorySlot("Wood", (StackIdx, 100)),
            UeFixtureBuilder.InventorySlot("Meta_Knife", (DurIdx, 420))));
        var stash = new MetaInventoryModel { InventoryId = "1" };

        var result = Service().Return(prospect, stash);

        stash.Items.Select(i => i.ItemStaticData!.RowName).Should().BeEquivalentTo(["Meta_Arrow", "Meta_Knife"]);
        result.SlotsRemoved.Should().Be(2);
        result.LeftBehind.Should().ContainSingle(i => i.RowName == "Wood" && i.TotalQuantity == 100);
        SlotsLeftIn(prospect).Should().Be(1, "only the Wood slot remains");
        ProspectBlobVerifier.VerifyHash(prospect.ProspectBlob).Should().BeTrue("the re-encoded world must pass the game's hash check");
    }

    [Fact]
    public void Return_GivesEachWorldSlotItsOwnStashEntry_RatherThanInventingStacks()
    {
        // Six arrows here and four there must not become one stack of ten: the game writes no stash
        // entry with a stack above 1 on any available save, so a merged stack is a shape it never made.
        var prospect = WrapWorld(World(
            UeFixtureBuilder.InventorySlot("Meta_Arrow", (StackIdx, 6)),
            UeFixtureBuilder.InventorySlot("Meta_Arrow", (StackIdx, 4))));
        var stash = new MetaInventoryModel { InventoryId = "1" };

        var result = Service().Return(prospect, stash);

        stash.Items.Should().HaveCount(2);
        stash.Items.Select(StashEditService.GetStack).Should().BeEquivalentTo([6, 4]);
        result.TotalQuantity.Should().Be(10);
    }

    [Fact]
    public void Return_KeepsAToolsWear_InsteadOfRepairingItForFree()
    {
        var prospect = WrapWorld(World(UeFixtureBuilder.InventorySlot("Meta_Knife", (DurIdx, 420))));
        var stash = new MetaInventoryModel { InventoryId = "1" };

        Service().Return(prospect, stash);

        StashEditService.GetDurability(stash.Items.Single()).Should().Be(420);
    }

    [Fact]
    public void Return_SplitsAnOversizedSlot_RatherThanClampingAwayTheDifference()
    {
        var prospect = WrapWorld(World(UeFixtureBuilder.InventorySlot("Meta_Arrow", (StackIdx, 250))));
        var stash = new MetaInventoryModel { InventoryId = "1" };

        Service().Return(prospect, stash);

        stash.Items.Sum(StashEditService.GetStack).Should().Be(250, "quantity is conserved");
        stash.Items.Max(StashEditService.GetStack).Should().BeLessThanOrEqualTo(StashEditService.MaxStack);
    }

    [Fact]
    public void Return_WithRowNameFilter_OnlyReturnsTheSelectedItems()
    {
        var prospect = WrapWorld(World(
            UeFixtureBuilder.InventorySlot("Meta_Arrow", (StackIdx, 10)),
            UeFixtureBuilder.InventorySlot("Meta_Bolt", (StackIdx, 20))));
        var stash = new MetaInventoryModel { InventoryId = "1" };

        Service().Return(prospect, stash, new HashSet<string> { "Meta_Arrow" });

        stash.Items.Should().ContainSingle();
        ProspectBlobVerifier.VerifyHash(prospect.ProspectBlob).Should().BeTrue();
        SlotsLeftIn(prospect).Should().Be(1, "the unselected slot stays in the prospect");
    }

    [Fact]
    public void ReturnPlayerOwned_ReturnsPlayerItems_ButLeavesMachineInventories()
    {
        var player = WrapWorld(UeFixtureBuilder.WorldWithSlots(
            "/Script/Icarus.PlayerStateRecorderComponent",
            new[] { UeFixtureBuilder.InventorySlot("Meta_Arrow", (StackIdx, 10)) }));
        var stash = new MetaInventoryModel { InventoryId = "1" };
        Service().ReturnPlayerOwned(player, stash);
        stash.Items.Sum(StashEditService.GetStack).Should().Be(10);
        SlotsLeftIn(player).Should().Be(0, "the player's carried workshop items are returned");

        var machine = WrapWorld(UeFixtureBuilder.WorldWithSlots(
            "/Script/Icarus.DrillRecorderComponent",
            new[] { UeFixtureBuilder.InventorySlot("Meta_Arrow", (StackIdx, 10)) }));
        var stash2 = new MetaInventoryModel { InventoryId = "1" };
        Service().ReturnPlayerOwned(machine, stash2);
        stash2.Items.Should().BeEmpty("a drill's inventory is not player-owned");
        SlotsLeftIn(machine).Should().Be(1, "the machine's items stay put");
    }

    [Fact]
    public void TheStashCheck_CannotBeSkipped()
    {
        var construct = () => new ProspectReturnService(new StashEditService(new SystemGuidProvider()), null!);
        construct.Should().Throw<ArgumentNullException>("there is no way to build a return that skips the stash check");
    }

    private static int SlotsLeftIn(ProspectFileModel prospect) =>
        new ProspectWorldEditor(UeBlob.Parse(ProspectBlobCodec.Decompress(prospect.ProspectBlob.BinaryBlob)))
            .FindItemSlots().Count;
}

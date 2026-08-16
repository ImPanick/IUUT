using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Gates the character inventory decode. The behaviour that matters for a game-accurate layout is
/// that slots are addressed by their stored grid position, not by their order in the file — a
/// character whose consumables sit at Location 0 and 2 must not be drawn as 0 and 1.
/// </summary>
public class ProspectInventoryReaderTests
{
    private const string PlayerA = "70000000000000001";

    private static byte[] Slot(int location, string row) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.IntProp("Location", location),
            UeFixtureBuilder.NameProp("ItemStaticData", row),
            UeFixtureBuilder.StrProp("ItemGuid", $"guid-{location}"));

    private static byte[] Inventory(int id, params byte[][] slots) =>
        UeFixtureBuilder.StructProp($"Inv{id}", "SavedInventory",
            UeFixtureBuilder.Concat(
                UeFixtureBuilder.IntProp("InventoryID", id),
                UeFixtureBuilder.StructArrayProp("Slots", "ItemSlot", slots)));

    private static byte[] World() =>
        UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob",
        [
            UeFixtureBuilder.Concat(
                UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.PlayerStateRecorderComponent"),
                UeFixtureBuilder.ByteStreamProp(
                    "BinaryData",
                    UeFixtureBuilder.StrProp("PlayerID", PlayerA),
                    UeFixtureBuilder.IntProp("ChrSlot", 1),
                    UeFixtureBuilder.RawStructProp("Location", "Vector", 0f, 0f, 0f),
                    UeFixtureBuilder.BoolProp("bIsAlive", true),
                    UeFixtureBuilder.IntProp("Health", 100),
                    UeFixtureBuilder.StructArrayProp("SavedInventories", "SavedInventory",
                    [
                        Inventory(2, Slot(0, "Steel_Axe"), Slot(3, "Player_Fist")),
                        Inventory(3, Slot(0, "Fur"), Slot(1, "Fire_Arrow")),
                        // The real-save shape that proves sparse storage: a gap at 1.
                        Inventory(4, Slot(0, "Oxygen_Tank"), Slot(2, "Thermos")),
                        Inventory(5,
                            Slot(0, "Meta_Carbon_Head_Alpha"),
                            Slot(5, "Envirosuit_Larkwell_Alpha"),
                            Slot(8, "Hunters_Backpack")),
                        Inventory(11, Slot(0, "Meta_Module_Temperature")),
                        Inventory(12, Slot(0, "Battery_Lantern")),
                    ]))),
        ]);

    [Fact]
    public void Read_IdentifiesEveryPanelTheGameDraws()
    {
        var data = World();
        var character = new ProspectCharacterReader().Read(data)[0];

        var inventories = new ProspectInventoryReader().Read(data, character);

        inventories.Select(i => i.Kind).Should().Equal(
            CharacterInventoryKind.Hotbar,
            CharacterInventoryKind.Bag,
            CharacterInventoryKind.Consumables,
            CharacterInventoryKind.Equipment,
            CharacterInventoryKind.Auxiliary,
            CharacterInventoryKind.Lantern);

        inventories.Single(i => i.Kind == CharacterInventoryKind.Auxiliary).Label.Should().Be("Auxiliary");
        inventories.Single(i => i.Kind == CharacterInventoryKind.Bag).Label.Should().Be("Inventory");
    }

    [Fact]
    public void Read_KeepsSlotsAtTheirStoredGridPosition()
    {
        var data = World();
        var character = new ProspectCharacterReader().Read(data)[0];

        var consumables = new ProspectInventoryReader().Read(data, character)
            .Single(i => i.Kind == CharacterInventoryKind.Consumables);

        consumables.Items.Select(i => i.Location).Should()
            .Equal([0, 2], "the middle slot is genuinely empty");
        consumables.OccupiedCount.Should().Be(2);
        consumables.MinimumCapacity.Should().Be(3, "a grid must be at least big enough to show slot 2");
    }

    [Fact]
    public void EquipmentSlots_AreNamedInTheGamesFixedOrder()
    {
        var data = World();
        var character = new ProspectCharacterReader().Read(data)[0];

        var equipment = new ProspectInventoryReader().Read(data, character)
            .Single(i => i.Kind == CharacterInventoryKind.Equipment);

        equipment.SlotName(0).Should().Be("Head");
        equipment.SlotName(5).Should().Be("Envirosuit");
        equipment.SlotName(8).Should().Be("Backpack");
        equipment.SlotName(9).Should().BeNull();

        equipment.Items.Single(i => i.Location == 5).RowName.Should().Be("Envirosuit_Larkwell_Alpha");
    }

    [Fact]
    public void SlotNames_AreOnlyMeaningfulOnTheEquipmentDoll()
    {
        var data = World();
        var character = new ProspectCharacterReader().Read(data)[0];

        var bag = new ProspectInventoryReader().Read(data, character)
            .Single(i => i.Kind == CharacterInventoryKind.Bag);

        bag.SlotName(0).Should().BeNull("a bag slot is a grid cell, not a body part");
    }

    [Fact]
    public void UnknownInventoryIds_AreSurfacedRatherThanDropped()
    {
        ProspectInventoryReader.Classify(99).Should().Be(CharacterInventoryKind.Other);
        new CharacterInventory(99, CharacterInventoryKind.Other, []).Label.Should().Be("Inventory 99");
    }
}

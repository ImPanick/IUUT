using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Gates the trapped-character rescue. This write frees a player whose body the game has stranded,
/// so it must move exactly the character asked for, carry their inventory with them (the items hang
/// off the same recorder), and never change the blob's length.
/// </summary>
public class ProspectCharacterEditorTests
{
    // Synthetic ids only — a real SteamID must never enter the repo (CONSTITUTION VII).
    private const string PlayerA = "70000000000000001";
    private const string PlayerB = "70000000000000002";

    private static byte[] Character(string playerId, int slot, bool alive, int health, float x, float y, float z, int items) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.PlayerStateRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.StrProp("PlayerID", playerId),
                UeFixtureBuilder.IntProp("ChrSlot", slot),
                UeFixtureBuilder.RawStructProp("Location", "Vector", x, y, z),
                UeFixtureBuilder.BoolProp("bIsAlive", alive),
                UeFixtureBuilder.IntProp("Health", health),
                UeFixtureBuilder.IntProp("RespawnCount", 2),
                UeFixtureBuilder.StructArrayProp("Slots", "ItemSlot",
                    [.. Enumerable.Range(0, items).Select(i => UeFixtureBuilder.IntProp("ItemID", i))])));

    private static byte[] World() =>
        UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob",
        [
            Character(PlayerA, 1, alive: false, health: 0, 100_00f, 200_00f, 50_00f, items: 3),
            Character(PlayerB, 2, alive: true, health: 300, 900_00f, 900_00f, 10_00f, items: 5),
        ]);

    [Fact]
    public void Read_ListsEveryCharacter_AndMasksTheSteamId()
    {
        var characters = new ProspectCharacterReader().Read(World());

        characters.Should().HaveCount(2);

        var trapped = characters[0];
        trapped.CharacterSlot.Should().Be(1);
        trapped.IsAlive.Should().BeFalse();
        trapped.Health.Should().Be(0);
        trapped.RespawnCount.Should().Be(2);
        trapped.CarriedSlots.Should().Be(3);
        trapped.HasCarriedItems.Should().BeTrue();
        trapped.Location!.Metres.Should().Be((100, 200, 50));

        trapped.MaskedPlayerId.Should().Be("…0001").And.NotContain(PlayerA);
    }

    [Fact]
    public void Rescue_MovesOnlyTheChosenCharacter_AndIsSizePreserving()
    {
        var data = World();
        var original = (byte[])data.Clone();
        var reader = new ProspectCharacterReader();
        var trapped = reader.Read(data)[0];

        var result = ProspectCharacterEditor.Rescue(data, trapped, 500, -250, 75);

        result.Moved.Should().BeTrue();
        result.Changed.Should().BeTrue();
        data.Length.Should().Be(original.Length, "Location is a native Vector — three floats, written in place");

        var differing = Enumerable.Range(0, data.Length).Count(i => data[i] != original[i]);
        differing.Should().BeLessThanOrEqualTo(12, "only the one character's three position floats change");

        var after = reader.Read(data);
        after[0].Location!.Metres.Should().Be((500, -250, 75));
        after[0].CarriedSlots.Should().Be(3, "the inventory travels with the body");
        after[1].Location!.Metres.Should().Be((900, 900, 10), "the other player is untouched");
        after[1].Health.Should().Be(300);
    }

    [Fact]
    public void Rescue_WithRevive_ClearsDeathAndRestoresEnoughHealthToStandUp()
    {
        var data = World();
        var reader = new ProspectCharacterReader();
        var trapped = reader.Read(data)[0];

        var result = ProspectCharacterEditor.Rescue(data, trapped, 0, 0, 0, revive: true);

        result.Revived.Should().BeTrue();

        var after = reader.Read(data)[0];
        after.IsAlive.Should().BeTrue();
        after.Health.Should().Be(ProspectCharacterEditor.ReviveHealth);
    }

    [Fact]
    public void Rescue_DoesNotLowerHealthOfAHealthyCharacter()
    {
        var data = World();
        var reader = new ProspectCharacterReader();
        var healthy = reader.Read(data)[1];

        ProspectCharacterEditor.Rescue(data, healthy, 1, 1, 1, revive: true);

        reader.Read(data)[1].Health.Should().Be(300, "reviving must never be a downgrade");
    }

    [Fact]
    public void Rescue_RefusesWhenTheRecordNoLongerMatchesThatPlayer()
    {
        var data = World();
        var original = (byte[])data.Clone();
        var trapped = new ProspectCharacterReader().Read(data)[0];

        // A stale index — the record at that slot belongs to somebody else now.
        var stale = trapped with { RecorderIndex = 1 };
        var result = ProspectCharacterEditor.Rescue(data, stale, 500, 500, 500);

        result.Changed.Should().BeFalse("moving the wrong player's body would be worse than doing nothing");
        data.Should().Equal(original);
    }
}

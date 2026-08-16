using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Gates player-name resolution. A rescue is a decision about a person, and an id fragment is not
/// a person — but a wrong name is worse than no name, so the pairing must be exact and the
/// fallback must never invent one.
/// </summary>
public class ProspectPlayerNamesTests
{
    // Synthetic ids and names only — real SteamIDs and personas never enter the repo.
    private const string IdA = "70000000000000001";
    private const string IdB = "70000000000000002";

    private static byte[] HistoryEntry(string userId, string name) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("UserID", userId),
            UeFixtureBuilder.StrProp("CachedCharacterName", name));

    private static byte[] Character(string playerId) =>
        UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.PlayerStateRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.StrProp("PlayerID", playerId),
                UeFixtureBuilder.IntProp("ChrSlot", 1),
                UeFixtureBuilder.RawStructProp("Location", "Vector", 0f, 0f, 0f),
                UeFixtureBuilder.BoolProp("bIsAlive", true),
                UeFixtureBuilder.IntProp("Health", 100)));

    private static byte[] World(params byte[][] historyEntries) =>
        UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob",
        [
            Character(IdA),
            Character(IdB),
            UeFixtureBuilder.Concat(
                UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.PlayerHistoryRecorderComponent"),
                UeFixtureBuilder.ByteStreamProp(
                    "BinaryData",
                    UeFixtureBuilder.StructArrayProp("SavedHistoryData", "PlayerHistoryEntry", historyEntries))),
        ]);

    [Fact]
    public void Read_PairsEachPlayerWithTheirOwnName()
    {
        var data = World(HistoryEntry(IdA, "Ash"), HistoryEntry(IdB, "Wren"));

        var names = ProspectPlayerNames.Read(data);

        names.Should().HaveCount(2);
        names[IdA].Should().Be("Ash");
        names[IdB].Should().Be("Wren", "pairing must survive walking multiple entries");
    }

    [Fact]
    public void Describe_UsesTheNameWhenTheWorldRemembersOne()
    {
        var data = World(HistoryEntry(IdA, "Ash"), HistoryEntry(IdB, "Wren"));
        var names = ProspectPlayerNames.Read(data);
        var characters = new ProspectCharacterReader().Read(data);

        ProspectPlayerNames.Describe(names, characters[0]).Should().Be("Ash");
        ProspectPlayerNames.Describe(names, characters[1]).Should().Be("Wren");
    }

    [Fact]
    public void Describe_FallsBackToAMaskedId_AndNeverInventsAName()
    {
        var data = World(HistoryEntry(IdA, "Ash"));
        var names = ProspectPlayerNames.Read(data);
        var unknown = new ProspectCharacterReader().Read(data)[1];

        var label = ProspectPlayerNames.Describe(names, unknown);

        label.Should().Be("Player …0002");
        label.Should().NotContain(IdB, "a raw SteamID must never reach the screen");
    }

    [Fact]
    public void Read_OnAWorldWithNoHistory_ReturnsEmptyRatherThanGuessing()
    {
        var data = UeFixtureBuilder.StructArrayProp("StateRecorderBlobs", "StateRecorderBlob", [Character(IdA)]);

        ProspectPlayerNames.Read(data).Should().BeEmpty();
    }

    [Fact]
    public void Read_IgnoresPlaceholderNames()
    {
        var data = World(HistoryEntry(IdA, "None"), HistoryEntry(IdB, "Wren"));

        var names = ProspectPlayerNames.Read(data);

        names.Should().ContainKey(IdB);
        names.Should().NotContainKey(IdA, "\"None\" is the engine's empty string, not somebody's name");
    }
}

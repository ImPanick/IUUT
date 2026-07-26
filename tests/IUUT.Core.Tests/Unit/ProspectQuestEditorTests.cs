using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Gates the quest-reset write (Tier 3, the first blob write beyond item slots): every write
/// must be in-place and size-preserving, touch EXACTLY the intended bytes (counted!), leave
/// every other recorder byte-identical, be idempotent, and round-trip through the blob codec.
/// </summary>
public class ProspectQuestEditorTests
{
    private static byte[] QuestWorld()
    {
        var manager = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.IcarusQuestManagerRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.NameProp("FactionMissionName", "STYX_TEST_Expedition"),
                UeFixtureBuilder.BoolProp("bMissionComplete", true)));

        var doneStep = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.IcarusQuestRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.NameProp("QuestName", "STYX_TEST_Deploy_Beacon"),
                UeFixtureBuilder.StructArrayProp("VariableRecords", "QuestVariableRecord",
                [
                    UeFixtureBuilder.Concat(
                        UeFixtureBuilder.StrProp("VariableName", "QuestComplete"),
                        UeFixtureBuilder.BoolProp("bVariable", true),
                        UeFixtureBuilder.IntProp("iVariable", 0)),
                    UeFixtureBuilder.Concat(
                        UeFixtureBuilder.StrProp("VariableName", "Count"),
                        UeFixtureBuilder.BoolProp("bVariable", false),
                        UeFixtureBuilder.IntProp("iVariable", 3)),
                ])));

        var mount = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.IcarusMountCharacterRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp("BinaryData", UeFixtureBuilder.StrProp("MountName", "Keep_Me")));

        return UeFixtureBuilder.StructArrayProp(
            "StateRecorderBlobs", "StateRecorderBlob", [manager, doneStep, mount]);
    }

    [Fact]
    public void Reset_TouchesExactlyTheIntendedBytes_AndNothingElse()
    {
        var data = QuestWorld();
        var original = (byte[])data.Clone();

        var result = ProspectQuestEditor.Reset(data);

        result.ManagerCleared.Should().BeTrue();
        result.StepsReset.Should().Be(1);
        result.VariablesCleared.Should().Be(2, "QuestComplete's bool and Count's int both change");

        data.Length.Should().Be(original.Length, "reset is size-preserving");
        var differing = Enumerable.Range(0, data.Length).Count(i => data[i] != original[i]);
        differing.Should().Be(3, "bMissionComplete tag byte + QuestComplete tag byte + one byte of Count's int32 (3 → 0)");

        var state = new ProspectQuestReader().Read(data);
        state.MissionComplete.Should().BeFalse();
        state.Steps.Should().OnlyContain(s => !s.IsComplete);
        state.Steps.SelectMany(s => s.Variables).Should().OnlyContain(v => !v.BoolValue && v.IntValue == 0);

        new ProspectMountReader().Read(data).Should().ContainSingle(m => m.Name == "Keep_Me", "other recorders are untouched");
    }

    [Fact]
    public void Reset_IsIdempotent()
    {
        var data = QuestWorld();
        ProspectQuestEditor.Reset(data);

        var second = ProspectQuestEditor.Reset(data);

        second.Changed.Should().BeFalse("a reset world has nothing left to clear");
    }

    [Fact]
    public void ResetMission_RoundTripsThroughTheBlobCodec()
    {
        var blob = new ProspectBlobModel();
        ProspectBlobCodec.SetUncompressed(blob, QuestWorld());
        var prospect = new ProspectFileModel { ProspectBlob = blob };

        var result = ProspectQuestEditor.ResetMission(prospect);

        result.Changed.Should().BeTrue();
        var reread = new ProspectQuestReader().Read(ProspectBlobCodec.Decompress(blob.BinaryBlob));
        reread.MissionComplete.Should().BeFalse();
        reread.Steps.Should().OnlyContain(s => !s.IsComplete);
    }
}

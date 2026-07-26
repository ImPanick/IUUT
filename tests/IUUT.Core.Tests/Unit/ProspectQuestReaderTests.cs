using FluentAssertions;
using IUUT.Core.Prospects.World;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the read-only quest-state decoder against synthetic recorder fixtures (no real
/// save data): the manager's mission name + completion flag, per-step names, and the
/// QuestComplete variable driving step completion — including a FALSE bool (the value byte
/// lives in the property tag, an easy place to misread).
/// </summary>
public class ProspectQuestReaderTests
{
    private static byte[] QuestWorld()
    {
        var manager = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.IcarusQuestManagerRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.NameProp("FactionMissionName", "STYX_TEST_Expedition"),
                UeFixtureBuilder.BoolProp("bMissionComplete", false)));

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

        var pendingStep = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.IcarusQuestRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp(
                "BinaryData",
                UeFixtureBuilder.NameProp("QuestName", "STYX_TEST_Activate_Scanner")));

        // An unrelated recorder that must be ignored.
        var other = UeFixtureBuilder.Concat(
            UeFixtureBuilder.StrProp("ComponentClassName", "/Script/Icarus.IcarusMountCharacterRecorderComponent"),
            UeFixtureBuilder.ByteStreamProp("BinaryData", UeFixtureBuilder.StrProp("MountName", "NotAQuest")));

        return UeFixtureBuilder.StructArrayProp(
            "StateRecorderBlobs", "StateRecorderBlob", [manager, doneStep, pendingStep, other]);
    }

    [Fact]
    public void Read_DecodesTheMissionAndItsSteps()
    {
        var state = new ProspectQuestReader().Read(QuestWorld());

        state.HasMission.Should().BeTrue();
        state.MissionName.Should().Be("STYX_TEST_Expedition");
        state.MissionComplete.Should().BeFalse("the tag-byte bool must read FALSE correctly");
        state.Steps.Should().HaveCount(2, "the mount recorder is not a quest");

        var done = state.Steps.Single(s => s.QuestName == "STYX_TEST_Deploy_Beacon");
        done.IsComplete.Should().BeTrue("its QuestComplete variable is true");
        done.Variables.Should().ContainSingle(v => v.Name == "Count").Which.IntValue.Should().Be(3);

        state.Steps.Single(s => s.QuestName == "STYX_TEST_Activate_Scanner")
            .IsComplete.Should().BeFalse("no QuestComplete variable means not complete");
    }

    [Fact]
    public void Read_NoRecorders_ReturnsAnEmptyState()
    {
        var state = new ProspectQuestReader().Read(UeFixtureBuilder.StrProp("Unrelated", "x"));

        state.HasMission.Should().BeFalse();
        state.Steps.Should().BeEmpty();
    }
}

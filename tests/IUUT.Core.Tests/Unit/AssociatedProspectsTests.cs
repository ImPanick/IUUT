using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class AssociatedProspectsTests
{
    // Nested-stringified container: outer key (the file name) → array of JSON-stringified objects.
    // Each inner object is wrapped in "AssociatedProspect" — the REAL on-disk shape (regression for bug #8,
    // where the missing wrapper left every ProspectID blank in the UI).
    private const string Sample =
        "{\"AssociatedProspects_Slot_1.json\":[" +
        "\"{\\\"AssociatedProspect\\\":{\\\"ProspectID\\\":\\\"Olympus_A\\\",\\\"ProspectState\\\":\\\"Active\\\"}}\"," +
        "\"{\\\"AssociatedProspect\\\":{\\\"ProspectID\\\":\\\"PGH-5\\\",\\\"ProspectState\\\":\\\"Completed\\\"}}\"]}";

    // The full real structure: ProspectID + members nested under the AssociatedProspect wrapper.
    private const string RealShape =
        "{\"AssociatedProspects_Slot_1.json\":[" +
        "\"{\\\"AssociatedProspect\\\":{\\\"ProspectID\\\":\\\"Olympus\\\",\\\"ClaimedAccountID\\\":\\\"00000000000000000\\\"," +
        "\\\"ProspectState\\\":\\\"Active\\\",\\\"AssociatedMembers\\\":[{\\\"CharacterName\\\":\\\"Pioneer\\\",\\\"ChrSlot\\\":2}]}}\"]}";

    private readonly ProspectEditService _service = new();

    [Fact]
    public void Parse_CapturesOuterKey_AndInnerObjects()
    {
        var model = AssociatedProspectsParser.Parse(Sample);

        model.ContainerKey.Should().Be("AssociatedProspects_Slot_1.json");
        model.Prospects.Select(p => p.ProspectId).Should().ContainInOrder("Olympus_A", "PGH-5");
        model.Prospects[0].AdditionalData.Should().ContainKey("ProspectState");
    }

    [Fact] // regression for bug #8: the "AssociatedProspect" wrapper left ProspectID blank in the UI.
    public void Parse_RealWrappedShape_PopulatesProspectId_AndPreservesMembers()
    {
        var model = AssociatedProspectsParser.Parse(RealShape);

        var prospect = model.Prospects.Should().ContainSingle().Subject;
        prospect.ProspectId.Should().Be("Olympus", "ProspectID is unwrapped from the AssociatedProspect wrapper, not blank");
        prospect.AdditionalData.Should().ContainKey("AssociatedMembers");

        // and it round-trips (re-wrapped) without losing the id or members
        var reparsed = AssociatedProspectsParser.Parse(AssociatedProspectsSerializer.Serialize(model));
        reparsed.Prospects.Should().ContainSingle().Which.ProspectId.Should().Be("Olympus");
        reparsed.Prospects[0].AdditionalData.Should().ContainKey("AssociatedMembers");
    }

    [Fact]
    public void RoundTrip_PreservesKeyAndInnerState()
    {
        var reparsed = AssociatedProspectsParser.Parse(
            AssociatedProspectsSerializer.Serialize(AssociatedProspectsParser.Parse(Sample)));

        reparsed.ContainerKey.Should().Be("AssociatedProspects_Slot_1.json");
        reparsed.Prospects.Should().HaveCount(2);
        reparsed.Prospects[1].AdditionalData.Should().ContainKey("ProspectState");
    }

    [Fact]
    public void Unstick_RemovesByProspectId_AndReportsPresence()
    {
        var model = AssociatedProspectsParser.Parse(Sample);

        _service.Unstick(model, "Olympus_A").Should().BeTrue();
        _service.Unstick(model, "Olympus_A").Should().BeFalse();
        model.Prospects.Should().ContainSingle(p => p.ProspectId == "PGH-5");
    }
}

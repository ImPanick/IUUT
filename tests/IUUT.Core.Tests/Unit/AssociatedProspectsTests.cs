using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class AssociatedProspectsTests
{
    // Nested-stringified container: outer key (the file name) → array of JSON-stringified objects.
    private const string Sample =
        "{\"AssociatedProspects_Slot_1.json\":[" +
        "\"{\\\"ProspectID\\\":\\\"Olympus_A\\\",\\\"ProspectState\\\":\\\"Active\\\"}\"," +
        "\"{\\\"ProspectID\\\":\\\"PGH-5\\\",\\\"ProspectState\\\":\\\"Completed\\\"}\"]}";

    private readonly ProspectEditService _service = new();

    [Fact]
    public void Parse_CapturesOuterKey_AndInnerObjects()
    {
        var model = AssociatedProspectsParser.Parse(Sample);

        model.ContainerKey.Should().Be("AssociatedProspects_Slot_1.json");
        model.Prospects.Select(p => p.ProspectId).Should().ContainInOrder("Olympus_A", "PGH-5");
        model.Prospects[0].AdditionalData.Should().ContainKey("ProspectState");
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

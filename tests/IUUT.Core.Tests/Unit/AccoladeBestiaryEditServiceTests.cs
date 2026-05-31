using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class AccoladeBestiaryEditServiceTests
{
    private readonly AccoladeBestiaryEditService _service = new(FixedClock.Default);

    [Fact]
    public void AddAccolade_AddsWithCorrectShapeAndTimestamp_ThenDedupes()
    {
        var accolades = new AccoladesModel();

        _service.AddAccolade(accolades, "Accolade_FirstSteps").Should().BeTrue();

        var entry = accolades.CompletedAccolades.Single();
        entry.Accolade.RowName.Should().Be("Accolade_FirstSteps");
        entry.Accolade.DataTableName.Should().Be("D_Accolades");
        entry.ProspectID.Should().Be("");
        entry.TimeCompleted.Should().Be("2020.01.01-00.00.00");

        _service.AddAccolade(accolades, "Accolade_FirstSteps").Should().BeFalse();
        accolades.CompletedAccolades.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveAccolade_ReportsPresence()
    {
        var accolades = new AccoladesModel();
        _service.AddAccolade(accolades, "Accolade_FirstSteps");

        _service.RemoveAccolade(accolades, "Accolade_FirstSteps").Should().BeTrue();
        _service.RemoveAccolade(accolades, "Accolade_FirstSteps").Should().BeFalse();
        accolades.CompletedAccolades.Should().BeEmpty();
    }

    [Fact]
    public void SetBestiaryPoints_AddsWhenAbsent_UpdatesWhenPresent()
    {
        var bestiary = new BestiaryModel();

        _service.SetBestiaryPoints(bestiary, "Forest_Wolf", 500);
        var entry = bestiary.BestiaryTracking.Single();
        entry.BestiaryGroup.RowName.Should().Be("Forest_Wolf");
        entry.BestiaryGroup.DataTableName.Should().Be("D_BestiaryData");
        entry.NumPoints.Should().Be(500);

        _service.SetBestiaryPoints(bestiary, "Forest_Wolf", 1046);
        bestiary.BestiaryTracking.Single().NumPoints.Should().Be(1046);
    }

    [Fact]
    public void RemoveBestiaryGroup_ReportsPresence()
    {
        var bestiary = new BestiaryModel();
        _service.SetBestiaryPoints(bestiary, "Forest_Wolf", 1);

        _service.RemoveBestiaryGroup(bestiary, "Forest_Wolf").Should().BeTrue();
        _service.RemoveBestiaryGroup(bestiary, "Forest_Wolf").Should().BeFalse();
        bestiary.BestiaryTracking.Should().BeEmpty();
    }
}

using FluentAssertions;
using IUUT.Core.Catalog;
using IUUT.Core.Models;
using IUUT.Core.Services;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies <see cref="MissionCompletionService"/> against the embedded mission graph: completing a
/// mission grants its <c>Prospect_*</c> talent plus the full transitive prerequisite closure, additively
/// and idempotently.
/// </summary>
public class MissionCompletionServiceTests
{
    private static readonly MissionCatalog _missions = GameCatalogs.LoadEmbedded().Missions;
    private static readonly string[] _forestScan = ["Prospect_OLY_Forest_Scan"];
    private static readonly string[] _unknownMission = ["Prospect_Not_A_Real_Mission"];

    private static MissionCompletionService NewService() => new(_missions);

    [Fact]
    public void Complete_GrantsTheMission_AndAllPrerequisites_AtRank1()
    {
        var profile = new ProfileModel();

        var result = NewService().Complete(profile, _forestScan);

        var rows = profile.Talents.Select(t => t.RowName).ToList();
        rows.Should().Contain("Prospect_OLY_Forest_Scan");
        rows.Should().Contain("Prospect_OLY_Forest_Exploration", "the prerequisite is auto-completed");
        profile.Talents.Should().OnlyContain(t => t.Rank == MissionCompletionService.MissionTalentRank);
        result.TalentsAdded.Should().Be(result.TalentsRequired, "a fresh profile gains every required talent");
    }

    [Fact]
    public void Complete_IsIdempotent()
    {
        var profile = new ProfileModel();
        var service = NewService();
        service.Complete(profile, _forestScan);
        var countAfterFirst = profile.Talents.Count;

        var second = service.Complete(profile, _forestScan);

        second.TalentsAdded.Should().Be(0);
        profile.Talents.Count.Should().Be(countAfterFirst);
    }

    [Fact]
    public void Complete_IsAdditive_PreservingExistingTalents()
    {
        var profile = new ProfileModel { Talents = { new Talent { RowName = "Workshop_Envirosuit", Rank = 1 } } };

        NewService().Complete(profile, _forestScan);

        profile.Talents.Should().Contain(t => t.RowName == "Workshop_Envirosuit", "unrelated talents are untouched");
    }

    [Fact]
    public void CompleteAll_GrantsEveryMission()
    {
        var profile = new ProfileModel();

        var result = NewService().CompleteAll(profile);

        result.TalentsAdded.Should().BeGreaterThanOrEqualTo(_missions.Count);
        _missions.Missions.Select(m => m.RowName).Should().BeSubsetOf(profile.Talents.Select(t => t.RowName));
        profile.Talents.Should().OnlyHaveUniqueItems(t => t.RowName);
    }

    [Fact]
    public void Complete__unknownMission_IsStillGranted()
    {
        var profile = new ProfileModel();

        var result = NewService().Complete(profile, _unknownMission);

        profile.Talents.Should().ContainSingle(t => t.RowName == "Prospect_Not_A_Real_Mission");
        result.TalentsAdded.Should().Be(1);
    }
}

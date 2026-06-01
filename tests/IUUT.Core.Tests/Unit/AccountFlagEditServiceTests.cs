using FluentAssertions;
using IUUT.Core.Catalog;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>Verifies name-based account-flag editing against the embedded D_AccountFlags catalog.</summary>
public class AccountFlagEditServiceTests
{
    private static readonly FlagCatalog _flags = GameCatalogs.LoadEmbedded().AccountFlags;
    private static AccountFlagEditService NewService() => new(_flags);

    [Fact]
    public void SetByName_EnablesAndClears_AKnownFlag()
    {
        var profile = new ProfileModel();
        var service = NewService();

        service.SetByName(profile, "GrantedTalent_Olympus_Nightfall", true).Should().BeTrue();
        profile.UnlockedFlags.Should().Contain(8, "id 8 is GrantedTalent_Olympus_Nightfall");

        service.SetByName(profile, "GrantedTalent_Olympus_Nightfall", false).Should().BeTrue();
        profile.UnlockedFlags.Should().NotContain(8);
    }

    [Fact]
    public void SetByName_UnknownName_DoesNothing()
    {
        var profile = new ProfileModel();

        NewService().SetByName(profile, "Not_A_Real_Flag", true).Should().BeFalse();
        profile.UnlockedFlags.Should().BeEmpty();
    }

    [Fact]
    public void SetById_IsIdempotent()
    {
        var profile = new ProfileModel();
        var service = NewService();

        service.SetById(profile, 8, true).Should().BeTrue();
        service.SetById(profile, 8, true).Should().BeFalse("already set");
        profile.UnlockedFlags.Count(f => f == 8).Should().Be(1);
    }

    [Fact]
    public void List_ReturnsEveryCatalogFlag_WithEnabledState()
    {
        var profile = new ProfileModel { UnlockedFlags = { 8 } };

        var list = NewService().List(profile);

        list.Count.Should().BeGreaterThanOrEqualTo(_flags.Ids.Count());
        list.Should().Contain(f => f.Id == 8 && f.Enabled, "id 8 is set");
        list.Should().Contain(f => f.Id == 9 && !f.Enabled, "id 9 is a known flag, not set");
    }

    [Fact]
    public void List_PreservesEnabledIdsBeyondTheCatalog()
    {
        var profile = new ProfileModel { UnlockedFlags = { 999 } };

        var list = NewService().List(profile);

        list.Should().Contain(f => f.Id == 999 && f.Enabled && f.Name == null,
            "an enabled id past the catalog snapshot stays visible, never dropped");
    }
}

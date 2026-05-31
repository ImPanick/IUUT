using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class AccountEditServiceTests
{
    private readonly AccountEditService _service = new();

    [Fact]
    public void SetCurrency_AddsWhenAbsent_UpdatesWhenPresent()
    {
        var profile = new ProfileModel();

        _service.SetCurrency(profile, "Credits", 100);
        profile.MetaResources.Should().ContainSingle(m => m.MetaRow == "Credits" && m.Count == 100);

        _service.SetCurrency(profile, "Credits", 250);
        profile.MetaResources.Should().ContainSingle(m => m.MetaRow == "Credits" && m.Count == 250);
    }

    [Fact]
    public void Flags_AddIsIdempotent_RemoveReportsPresence()
    {
        var profile = new ProfileModel();

        _service.AddFlag(profile, 7).Should().BeTrue();
        _service.AddFlag(profile, 7).Should().BeFalse();
        profile.UnlockedFlags.Should().ContainSingle(f => f == 7);

        _service.RemoveFlag(profile, 7).Should().BeTrue();
        _service.RemoveFlag(profile, 7).Should().BeFalse();
        profile.UnlockedFlags.Should().BeEmpty();
    }

    [Fact]
    public void SetWorkshopUnlock_AddsAtRankOne_Idempotent_ThenRemoves()
    {
        var profile = new ProfileModel();

        _service.SetWorkshopUnlock(profile, "Workshop_Envirosuit", unlocked: true);
        _service.SetWorkshopUnlock(profile, "Workshop_Envirosuit", unlocked: true);
        profile.Talents.Should().ContainSingle(t => t.RowName == "Workshop_Envirosuit" && t.Rank == 1);

        _service.SetWorkshopUnlock(profile, "Workshop_Envirosuit", unlocked: false);
        profile.Talents.Should().BeEmpty();
    }
}

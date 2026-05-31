using FluentAssertions;
using IUUT.Core.Editing;
using IUUT.Core.Models;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class CharacterEditServiceTests
{
    private readonly CharacterEditService _service = new();

    private static CharacterModel NewCharacter() => new() { CharacterName = "A", ChrSlot = 1, XP = 10, XP_Debt = 5, IsDead = true };

    [Fact]
    public void ScalarSetters_SetValues()
    {
        var c = NewCharacter();

        _service.SetExperience(c, 1_000);
        _service.SetDebt(c, 0);
        _service.SetDead(c, false);
        _service.SetAbandoned(c, false);
        _service.Rename(c, "Renamed");

        c.XP.Should().Be(1_000);
        c.XP_Debt.Should().Be(0);
        c.IsDead.Should().BeFalse();
        c.IsAbandoned.Should().BeFalse();
        c.CharacterName.Should().Be("Renamed");
    }

    [Fact]
    public void Rename_EmptyName_Throws()
    {
        var c = NewCharacter();
        var act = () => _service.Rename(c, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetTalentRank_AddsUpdatesAndRemovesAtZero()
    {
        var c = NewCharacter();

        _service.SetTalentRank(c, "Survival_HealthBoost", 2);
        c.Talents.Should().ContainSingle(t => t.RowName == "Survival_HealthBoost" && t.Rank == 2);

        _service.SetTalentRank(c, "Survival_HealthBoost", 4);
        c.Talents.Single().Rank.Should().Be(4);

        _service.SetTalentRank(c, "Survival_HealthBoost", 0);
        c.Talents.Should().BeEmpty();
    }

    [Fact]
    public void MaxTalents_MaxesExisting_AddsGenetics_PreservesReroute_AndLeavesXpAndDeath()
    {
        var c = NewCharacter();
        c.Talents.Add(new Talent { RowName = "Survival_HealthBoost", Rank = 1 });
        c.Talents.Add(new Talent { RowName = "Genetics_Mutation_Reroute", Rank = 0 });

        _service.MaxTalents(c);

        c.Talents.Should().Contain(t => t.RowName == "Survival_HealthBoost" && t.Rank == 4);
        c.Talents.Where(t => t.RowName.StartsWith("Genetics_", StringComparison.Ordinal) && !t.RowName.Contains("Reroute", StringComparison.Ordinal))
            .Should().HaveCount(16);
        c.Talents.Should().ContainSingle(t => t.RowName == "Genetics_Mutation_Reroute")
            .Which.Rank.Should().Be(0, "visual reroute nodes are not bumped");

        // MaxTalents must not touch XP / debt / death (that's Lazy Max's job).
        c.XP.Should().Be(10);
        c.XP_Debt.Should().Be(5);
        c.IsDead.Should().BeTrue();
    }
}

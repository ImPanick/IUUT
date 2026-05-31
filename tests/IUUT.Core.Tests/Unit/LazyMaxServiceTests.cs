using FluentAssertions;
using IUUT.Core.Catalog;
using IUUT.Core.Models;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using IUUT.Core.Validation;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class LazyMaxServiceTests
{
    private static LazyMaxService NewService() =>
        new(GameCatalogs.LoadEmbedded(), FixedClock.Default);

    private static CharacterModel Character(int slot, params (string Row, int Rank)[] talents) => new()
    {
        CharacterName = $"Char{slot}",
        ChrSlot = slot,
        XP = 100,
        XP_Debt = 50,
        IsDead = true,
        IsAbandoned = true,
        Talents = talents.Select(t => new Talent { RowName = t.Row, Rank = t.Rank }).ToList(),
    };

    // ---- Characters -------------------------------------------------------

    [Fact]
    public void MaxCharacters_AppliesAccountUnionAtRank4_ToEveryCharacter()
    {
        var a = Character(1, ("Survival_HealthBoost", 2));
        var b = Character(2, ("Crafting_Speed", 1));
        var characters = new[] { a, b };

        var unionSize = NewService().MaxCharacters(characters);

        foreach (var c in characters)
        {
            // Each character ends up with the full union: the other slot's talent too.
            c.Talents.Should().Contain(t => t.RowName == "Survival_HealthBoost" && t.Rank == 4);
            c.Talents.Should().Contain(t => t.RowName == "Crafting_Speed" && t.Rank == 4);
            // ...plus every functional Genetics row at rank 4.
            c.Talents.Should().Contain(t => t.RowName == "Genetics_Twins" && t.Rank == 4);
            c.Talents.Should().Contain(t => t.RowName == "Genetics_Lineage" && t.Rank == 4);
            // No duplicate RowNames (would be a hard validation error).
            c.Talents.Select(t => t.RowName).Should().OnlyHaveUniqueItems();
            c.Talents.Should().HaveCount(unionSize);
        }
    }

    [Fact]
    public void MaxCharacters_OverlaysSixteenGeneticsRows()
    {
        var character = Character(1);

        NewService().MaxCharacters(new[] { character });

        var genetics = character.Talents.Where(t => t.RowName.StartsWith("Genetics_", StringComparison.Ordinal)).ToList();
        genetics.Should().HaveCount(16);
        genetics.Should().OnlyContain(t => t.Rank == 4);
    }

    [Fact]
    public void MaxCharacters_RaisesXp_ClearsDebt_AndRevives()
    {
        var character = Character(1);

        NewService().MaxCharacters(new[] { character });

        character.XP.Should().Be(LazyMaxService.MinMaxedExperience);
        character.XP_Debt.Should().Be(0);
        character.IsDead.Should().BeFalse();
        character.IsAbandoned.Should().BeFalse();
    }

    [Fact]
    public void MaxCharacters_NeverLowersAlreadyHigherXp()
    {
        var character = Character(1);
        character.XP = LazyMaxService.MinMaxedExperience + 5_000_000;

        NewService().MaxCharacters(new[] { character });

        character.XP.Should().Be(LazyMaxService.MinMaxedExperience + 5_000_000);
    }

    [Fact]
    public void MaxCharacters_PreservesExistingRerouteNode_AndNeverAddsReroute()
    {
        var character = Character(1, ("Genetics_Mutation_Reroute", 0), ("Survival_HealthBoost", 1));

        NewService().MaxCharacters(new[] { character });

        // The pre-existing visual node is kept verbatim (rank untouched, not bumped to 4).
        character.Talents.Should().ContainSingle(t => t.RowName == "Genetics_Mutation_Reroute")
            .Which.Rank.Should().Be(0);
        // No *Reroute* row is ever introduced by the union.
        character.Talents.Count(t => t.RowName.Contains("Reroute", StringComparison.Ordinal)).Should().Be(1);
    }

    [Fact]
    public void MaxCharacters_PreservesUnknownTalentFields_OnExistingRows()
    {
        var character = Character(1);
        var talent = new Talent { RowName = "Survival_HealthBoost", Rank = 1 };
        talent.AdditionalData = new() { ["Mystery"] = System.Text.Json.JsonSerializer.SerializeToElement(7) };
        character.Talents.Add(talent);

        NewService().MaxCharacters(new[] { character });

        var preserved = character.Talents.Single(t => t.RowName == "Survival_HealthBoost");
        preserved.Rank.Should().Be(4);
        preserved.AdditionalData.Should().ContainKey("Mystery");
    }

    // ---- Profile ----------------------------------------------------------

    [Fact]
    public void MaxProfile_RaisesEveryCurrency_AndAddsCatalogCurrencies()
    {
        var profile = new ProfileModel
        {
            MetaResources = [new MetaResource { MetaRow = "Credits", Count = 5 }],
        };

        var (metaMaxed, _, _) = NewService().MaxProfile(profile);

        profile.MetaResources.Should().OnlyContain(m => m.Count >= LazyMaxService.MaxedMetaResourceCount);
        profile.MetaResources.Select(m => m.MetaRow).Should().Contain("Credits");
        metaMaxed.Should().Be(profile.MetaResources.Count);
        // The seven D_MetaResources rows are all present.
        profile.MetaResources.Should().HaveCountGreaterThanOrEqualTo(7);
    }

    [Fact]
    public void MaxProfile_NeverLowersHigherCurrency()
    {
        var profile = new ProfileModel
        {
            MetaResources = [new MetaResource { MetaRow = "Credits", Count = 5_000_000 }],
        };

        NewService().MaxProfile(profile);

        profile.MetaResources.Single(m => m.MetaRow == "Credits").Count.Should().Be(5_000_000);
    }

    [Fact]
    public void MaxProfile_UnlocksAllWorkshopAndProspectAtRankOne()
    {
        var profile = new ProfileModel
        {
            Talents = [new Talent { RowName = "Some_NonCatalog_Unlock", Rank = 1 }],
        };

        var (_, workshopTotal, workshopAdded) = NewService().MaxProfile(profile);

        workshopTotal.Should().BeGreaterThan(300);
        workshopAdded.Should().Be(workshopTotal, "the profile started with no workshop/prospect unlocks");
        profile.Talents
            .Where(t => t.RowName.StartsWith("Workshop_", StringComparison.Ordinal) ||
                        t.RowName.StartsWith("Prospect_", StringComparison.Ordinal))
            .Should().OnlyContain(t => t.Rank == LazyMaxService.WorkshopUnlockRank);
        // The pre-existing non-catalog unlock is preserved.
        profile.Talents.Should().Contain(t => t.RowName == "Some_NonCatalog_Unlock");
    }

    // ---- Accolades --------------------------------------------------------

    [Fact]
    public void MaxAccolades_AppendsAllMissing_WithTimestampAndEmptyProspect()
    {
        var accolades = new AccoladesModel();

        var added = NewService().MaxAccolades(accolades);

        added.Should().Be(accolades.CompletedAccolades.Count).And.BeGreaterThan(200);
        accolades.CompletedAccolades.Should().OnlyContain(a =>
            a.Accolade.DataTableName == "D_Accolades" &&
            a.ProspectID == "" &&
            a.TimeCompleted == "2020.01.01-00.00.00");
        accolades.CompletedAccolades.Select(a => a.Accolade.RowName).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void MaxAccolades_DoesNotDuplicateExistingAccolades()
    {
        var accolades = new AccoladesModel();
        var service = NewService();
        var first = service.MaxAccolades(accolades);

        var second = service.MaxAccolades(accolades);

        second.Should().Be(0, "every catalog accolade is already present after the first pass");
        accolades.CompletedAccolades.Should().HaveCount(first);
    }

    // ---- Bestiary ---------------------------------------------------------

    [Fact]
    public void MaxBestiary_MaxesAllGroups_AndPreservesFishTracking()
    {
        var bestiary = new BestiaryModel
        {
            BestiaryTracking =
            [
                new BestiaryEntry
                {
                    BestiaryGroup = new DataTableRef { RowName = "Forest_Wolf", DataTableName = "D_BestiaryData" },
                    NumPoints = 5,
                },
            ],
            FishTracking = [new FishEntry()],
        };

        var (total, added) = NewService().MaxBestiary(bestiary);

        bestiary.BestiaryTracking.Should().OnlyContain(b => b.NumPoints >= LazyMaxService.MaxedBestiaryPoints);
        bestiary.BestiaryTracking.Should().Contain(b => b.BestiaryGroup.RowName == "Forest_Wolf");
        total.Should().Be(bestiary.BestiaryTracking.Count).And.BeGreaterThan(70);
        added.Should().Be(total - 1, "one group (Forest_Wolf) already existed");
        bestiary.FishTracking.Should().HaveCount(1, "FishTracking is preserved verbatim");
    }

    // ---- MaxAll + validation gate ----------------------------------------

    [Fact]
    public void MaxAll_ReturnsPerFileSummaryCounts()
    {
        var profile = new ProfileModel { UserId = "76561198000000000" };
        var characters = new[] { Character(1), Character(2) };
        var accolades = new AccoladesModel();
        var bestiary = new BestiaryModel();

        var result = NewService().MaxAll(profile, characters, accolades, bestiary);

        result.CharactersMaxed.Should().Be(2);
        result.TalentsPerCharacter.Should().Be(16, "two empty characters yield only the 16 Genetics rows");
        result.MetaResourcesMaxed.Should().Be(7);
        result.WorkshopUnlocksTotal.Should().BeGreaterThan(300);
        result.WorkshopUnlocksAdded.Should().Be(result.WorkshopUnlocksTotal);
        result.AccoladesAdded.Should().BeGreaterThan(200);
        result.BestiaryGroupsTotal.Should().BeGreaterThan(70);
        result.BestiaryGroupsAdded.Should().Be(result.BestiaryGroupsTotal);
    }

    [Fact]
    public void MaxAll_Output_PassesValidationEngine()
    {
        const string steamId = "76561198000000000";
        var profile = new ProfileModel
        {
            UserId = steamId,
            NextChrSlot = 99,
            MetaResources = [new MetaResource { MetaRow = "Credits", Count = 10 }],
        };
        var characters = new[]
        {
            Character(1, ("Survival_HealthBoost", 1)),
            Character(2, ("Crafting_Speed", 2)),
        };

        NewService().MaxAll(profile, characters, new AccoladesModel(), new BestiaryModel());

        // Lazy Max output must clear the pre-write gate (master §13 / WP-12 contract).
        ValidationEngine.ValidateProfile(profile, steamId).IsValid.Should().BeTrue();
        var charResult = ValidationEngine.ValidateCharacters(characters, profile);
        charResult.HasErrors.Should().BeFalse();
        charResult.Issues.Should().NotContain(i => i.Code == "character-overranked-talent",
            "rank 4 is the highest observed rank, so maxing must not trip the over-rank warning");
    }
}

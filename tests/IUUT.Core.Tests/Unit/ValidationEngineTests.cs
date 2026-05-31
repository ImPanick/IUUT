using System.Text;
using FluentAssertions;
using IUUT.Core.Models;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using IUUT.Core.Validation;
using Xunit;

namespace IUUT.Core.Tests.Unit;

public class ValidationEngineTests
{
    private const string Folder = "00000000000000000";

    // ---- Profile ----

    [Fact]
    public void ValidateProfile_MatchingUserId_IsValid()
    {
        var profile = new ProfileModel { UserId = Folder };

        ValidationEngine.ValidateProfile(profile, Folder).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateProfile_UserIdMismatch_IsError()
    {
        var profile = new ProfileModel { UserId = "76561190000000000" };

        var result = ValidationEngine.ValidateProfile(profile, Folder);

        result.HasErrors.Should().BeTrue();
        result.Errors.Should().Contain(i => i.Code == "profile-userid-mismatch");
    }

    [Fact]
    public void ValidateProfile_EmptyUserId_NotAnError()
    {
        // A fresh/template profile may have no UserID yet.
        ValidationEngine.ValidateProfile(new ProfileModel { UserId = "" }, Folder).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateProfile_DuplicateMetaRowAndNegativeCount_AreWarnings()
    {
        var profile = new ProfileModel
        {
            UserId = Folder,
            MetaResources =
            [
                new MetaResource { MetaRow = "Credits", Count = 10 },
                new MetaResource { MetaRow = "Credits", Count = 20 },
                new MetaResource { MetaRow = "Refund", Count = -5 },
            ],
        };

        var result = ValidationEngine.ValidateProfile(profile, Folder);

        result.IsValid.Should().BeTrue("these are warnings, not errors");
        result.Warnings.Select(w => w.Code).Should().Contain(["profile-duplicate-metarow", "profile-negative-currency"]);
    }

    // ---- Characters ----

    [Fact]
    public void ValidateCharacters_UniqueSlotsAndTalents_IsValid()
    {
        var roster = new List<CharacterModel>
        {
            new() { ChrSlot = 1, Talents = [new Talent { RowName = "Hunting_Tier1", Rank = 1 }] },
            new() { ChrSlot = 2, Talents = [new Talent { RowName = "Combat_Tier1", Rank = 4 }] },
        };

        ValidationEngine.ValidateCharacters(roster).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateCharacters_DuplicateChrSlot_IsError()
    {
        var roster = new List<CharacterModel> { new() { ChrSlot = 1 }, new() { ChrSlot = 1 } };

        var result = ValidationEngine.ValidateCharacters(roster);

        result.Errors.Should().Contain(i => i.Code == "characters-duplicate-chrslot");
    }

    [Fact]
    public void ValidateCharacters_DuplicateTalent_IsError()
    {
        var roster = new List<CharacterModel>
        {
            new()
            {
                ChrSlot = 1,
                Talents =
                [
                    new Talent { RowName = "Hunting_Tier1", Rank = 1 },
                    new Talent { RowName = "Hunting_Tier1", Rank = 2 },
                ],
            },
        };

        ValidationEngine.ValidateCharacters(roster).Errors
            .Should().Contain(i => i.Code == "character-duplicate-talent");
    }

    [Fact]
    public void ValidateCharacters_OverRankedTalent_IsWarning()
    {
        var roster = new List<CharacterModel>
        {
            new() { ChrSlot = 1, Talents = [new Talent { RowName = "Hunting_Tier1", Rank = 5 }] },
        };

        var result = ValidationEngine.ValidateCharacters(roster);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(i => i.Code == "character-overranked-talent");
    }

    [Fact]
    public void ValidateCharacters_SlotAtOrAboveNextChrSlot_IsWarning()
    {
        var roster = new List<CharacterModel> { new() { ChrSlot = 4 } };
        var profile = new ProfileModel { NextChrSlot = 4 };

        ValidationEngine.ValidateCharacters(roster, profile).Warnings
            .Should().Contain(i => i.Code == "character-chrslot-exceeds-next");
    }

    // ---- Prospect blob ----

    [Fact]
    public void ValidateProspectBlob_ValidHash_IsValid()
    {
        var (base64, hash) = ProspectBlobFactory.Build(Encoding.UTF8.GetBytes("payload"));
        var blob = new ProspectBlobModel { BinaryBlob = base64, Hash = hash };

        ValidationEngine.ValidateProspectBlob(blob).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateProspectBlob_BadHash_IsError()
    {
        var (base64, _) = ProspectBlobFactory.Build(Encoding.UTF8.GetBytes("payload"));
        var blob = new ProspectBlobModel { BinaryBlob = base64, Hash = "0000" };

        ValidationEngine.ValidateProspectBlob(blob).Errors
            .Should().Contain(i => i.Code == "prospect-blob-hash-mismatch");
    }

    // ---- GUIDs ----

    [Fact]
    public void ValidateUniqueDatabaseGuids_Duplicate_IsError()
    {
        ValidationEngine.ValidateUniqueDatabaseGuids(["AAA", "BBB", "aaa"]).Errors
            .Should().Contain(i => i.Code == "duplicate-database-guid");
    }

    [Fact]
    public void ValidateUniqueDatabaseGuids_AllUnique_IsValid()
    {
        ValidationEngine.ValidateUniqueDatabaseGuids(["AAA", "BBB", "CCC"]).IsValid.Should().BeTrue();
    }

    // ---- Game state ----

    [Fact]
    public void ValidateGameState_Running_IsWarningNeverError()
    {
        var running = new GameDetectionResult { IsRunning = true, MatchedProcessNames = ["Icarus-3.0.12.152317-Shipping-DangerousHorizons"] };

        var result = ValidationEngine.ValidateGameState(running);

        result.HasErrors.Should().BeFalse("game state must never hard-block (CONSTITUTION IX)");
        result.Warnings.Should().Contain(i => i.Code == "game-running");
    }

    [Fact]
    public void ValidateGameState_NotRunning_IsValid()
    {
        ValidationEngine.ValidateGameState(GameDetectionResult.NotRunning).Issues.Should().BeEmpty();
    }

    // ---- Result combination ----

    [Fact]
    public void Combine_MergesIssues_AndErrorsBlock()
    {
        var combined = ValidationResult.Combine(
            ValidationEngine.ValidateProfile(new ProfileModel { UserId = "wrong" }, Folder),
            ValidationEngine.ValidateGameState(new GameDetectionResult { IsRunning = true, MatchedProcessNames = ["Icarus-X-Shipping"] }));

        combined.HasErrors.Should().BeTrue();
        combined.Errors.Should().ContainSingle();
        combined.Warnings.Should().ContainSingle();
        combined.IsValid.Should().BeFalse();
    }
}

using FluentAssertions;
using IUUT.Core.Abstractions;
using IUUT.Core.Editing;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class CustomApplyServiceTests : IDisposable
{
    private const string SteamId = "11111111111111111";

    private readonly TempDir _temp = new();
    private readonly string _dir;
    private readonly CustomApplyService _service = new(
        new SafeSaveWriter(new BackupManager(FixedClock.Default), new SystemGuidProvider()));

    public CustomApplyServiceTests()
    {
        _dir = Directory.CreateDirectory(Path.Combine(_temp.Path, SteamId)).FullName;
        File.WriteAllText(Path.Combine(_dir, "Profile.json"), ProfileSerializer.Serialize(new ProfileModel { UserId = SteamId, NextChrSlot = 99 }));
        File.WriteAllText(Path.Combine(_dir, "Characters.json"), CharactersSerializer.Serialize(new List<CharacterModel> { new() { CharacterName = "A", ChrSlot = 1 } }));
        File.WriteAllText(Path.Combine(_dir, "Accolades.json"), AccoladesSerializer.Serialize(new AccoladesModel()));
        File.WriteAllText(Path.Combine(_dir, "BestiaryData.json"), BestiarySerializer.Serialize(new BestiaryModel()));
    }

    private string Read(string name) => File.ReadAllText(Path.Combine(_dir, name));

    [Fact]
    public async Task PreviewAsync_EditTouchingOnlyProfile_MarksOnlyProfileChanged()
    {
        var plan = await _service.PreviewAsync(_dir,
            b => b.Profile.MetaResources.Add(new MetaResource { MetaRow = "Credits", Count = 500 }));

        plan.CanApply.Should().BeTrue();
        plan.HasChanges.Should().BeTrue();
        plan.ChangedFiles.Should().ContainSingle().Which.FileName.Should().Be("Profile.json");
    }

    [Fact]
    public async Task PreviewAsync_NoOpEdit_HasNoChanges()
    {
        var plan = await _service.PreviewAsync(_dir, _ => { });

        plan.CanApply.Should().BeTrue();
        plan.HasChanges.Should().BeFalse();
        plan.ChangedFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_WritesOnlyChangedFiles()
    {
        var plan = await _service.PreviewAsync(_dir,
            b => b.Profile.MetaResources.Add(new MetaResource { MetaRow = "Credits", Count = 500 }));

        var report = await _service.ApplyAsync(plan);

        report.Applied.Should().BeTrue();
        report.FileResults.Should().ContainSingle().Which.Ok.Should().BeTrue();

        ProfileParser.Parse(Read("Profile.json")).MetaResources.Should().ContainSingle(m => m.MetaRow == "Credits" && m.Count == 500);

        // Unchanged files are never written → no backup created for them.
        Directory.GetFiles(_dir, "Profile.json" + BackupManager.BackupInfix + "*").Should().NotBeEmpty();
        Directory.GetFiles(_dir, "Characters.json" + BackupManager.BackupInfix + "*").Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_NoOpEdit_WritesNothing_ButSucceeds()
    {
        var plan = await _service.PreviewAsync(_dir, _ => { });

        var report = await _service.ApplyAsync(plan);

        report.Applied.Should().BeTrue();
        report.FileResults.Should().BeEmpty();
        Directory.GetFiles(_dir, "*" + BackupManager.BackupInfix + "*").Should().BeEmpty();
    }

    [Fact]
    public async Task PreviewAsync_EditIntroducingValidationError_CannotApply_AndApplyWritesNothing()
    {
        var plan = await _service.PreviewAsync(_dir, b => b.Profile.UserId = "99999999999999999");

        plan.CanApply.Should().BeFalse();
        plan.Validation.Errors.Should().Contain(i => i.Code == "profile-userid-mismatch");

        var report = await _service.ApplyAsync(plan);
        report.Applied.Should().BeFalse();
        report.FileResults.Should().BeEmpty();
        ProfileParser.Parse(Read("Profile.json")).UserId.Should().Be(SteamId, "the bad edit was never written");
    }

    [Fact]
    public async Task PreviewAsync_MissingFile_CannotApply()
    {
        File.Delete(Path.Combine(_dir, "BestiaryData.json"));

        var plan = await _service.PreviewAsync(_dir, _ => { });

        plan.CanApply.Should().BeFalse();
        plan.Validation.Errors.Should().Contain(i => i.Code == "file-missing");
    }

    [Fact]
    public async Task LoadAsync_ParsesTheFourFiles()
    {
        var bundle = await _service.LoadAsync(_dir);

        bundle.Should().NotBeNull();
        bundle!.Profile.UserId.Should().Be(SteamId);
        bundle.Characters.Should().ContainSingle(c => c.CharacterName == "A");
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsNull()
    {
        File.Delete(Path.Combine(_dir, "Characters.json"));

        var bundle = await _service.LoadAsync(_dir);

        bundle.Should().BeNull();
    }

    [Fact]
    public async Task PreviewBundleAsync_EditedBundle_MarksOnlyChangedFiles_AndApplies()
    {
        var bundle = await _service.LoadAsync(_dir);
        bundle!.Profile.MetaResources.Add(new MetaResource { MetaRow = "Credits", Count = 1234 });

        var plan = await _service.PreviewBundleAsync(_dir, bundle);

        plan.CanApply.Should().BeTrue();
        plan.ChangedFiles.Should().ContainSingle().Which.FileName.Should().Be("Profile.json");

        var report = await _service.ApplyAsync(plan);
        report.Applied.Should().BeTrue();
        ProfileParser.Parse(Read("Profile.json")).MetaResources.Should().ContainSingle(m => m.MetaRow == "Credits" && m.Count == 1234);
    }

    [Fact]
    public async Task PreviewBundleAsync_UnmutatedBundle_HasNoChanges()
    {
        var bundle = await _service.LoadAsync(_dir);

        var plan = await _service.PreviewBundleAsync(_dir, bundle!);

        plan.CanApply.Should().BeTrue();
        plan.HasChanges.Should().BeFalse();
    }

    [Fact]
    public async Task PreviewBundleAsync_EditIntroducingValidationError_CannotApply()
    {
        var bundle = await _service.LoadAsync(_dir);
        bundle!.Profile.UserId = "99999999999999999";

        var plan = await _service.PreviewBundleAsync(_dir, bundle);

        plan.CanApply.Should().BeFalse();
        plan.Validation.Errors.Should().Contain(i => i.Code == "profile-userid-mismatch");
    }

    [Fact]
    public async Task PreviewBundleAsync_CharacterEditViaService_MarksOnlyCharactersChanged_AndApplies()
    {
        var characters = new CharacterEditService();
        var bundle = await _service.LoadAsync(_dir);
        var character = bundle!.Characters.Single();
        characters.SetExperience(character, 80_000_000);
        characters.SetTalentRank(character, "Survival_Tier1", 4);

        var plan = await _service.PreviewBundleAsync(_dir, bundle);

        plan.CanApply.Should().BeTrue();
        plan.ChangedFiles.Should().ContainSingle().Which.FileName.Should().Be("Characters.json");

        var report = await _service.ApplyAsync(plan);
        report.Applied.Should().BeTrue();

        var reloaded = CharactersParser.Parse(Read("Characters.json")).Single();
        reloaded.XP.Should().Be(80_000_000);
        reloaded.Talents.Should().Contain(t => t.RowName == "Survival_Tier1" && t.Rank == 4);
    }

    [Fact]
    public async Task PreviewBundleAsync_AccoladeAndBestiaryEditViaService_MarksBothChanged_AndApplies()
    {
        var editor = new AccoladeBestiaryEditService(FixedClock.Default);
        var bundle = await _service.LoadAsync(_dir);
        editor.AddAccolade(bundle!.Accolades, "Accolade_FirstSteps");
        editor.SetBestiaryPoints(bundle.Bestiary, "Bestiary_Wolf", 10_000);

        var plan = await _service.PreviewBundleAsync(_dir, bundle);

        plan.CanApply.Should().BeTrue();
        plan.ChangedFiles.Select(f => f.FileName).Should().BeEquivalentTo("Accolades.json", "BestiaryData.json");

        var report = await _service.ApplyAsync(plan);
        report.Applied.Should().BeTrue();

        AccoladesParser.Parse(Read("Accolades.json")).CompletedAccolades
            .Should().Contain(a => a.Accolade.RowName == "Accolade_FirstSteps");
        BestiaryParser.Parse(Read("BestiaryData.json")).BestiaryTracking
            .Should().Contain(b => b.BestiaryGroup.RowName == "Bestiary_Wolf" && b.NumPoints == 10_000);
    }

    public void Dispose() => _temp.Dispose();
}

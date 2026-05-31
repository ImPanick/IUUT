using FluentAssertions;
using IUUT.Core.Abstractions;
using IUUT.Core.Catalog;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Serializers;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class LazyMaxApplyServiceTests : IDisposable
{
    private const string SteamId = "11111111111111111";

    private readonly TempDir _temp = new();

    private static LazyMaxApplyService NewService(ISafeSaveWriter writer) =>
        new(new LazyMaxService(GameCatalogs.LoadEmbedded(), FixedClock.Default), writer);

    private string CreateSave(string steamId = SteamId, string? userId = null, int characters = 2)
    {
        var dir = Directory.CreateDirectory(Path.Combine(_temp.Path, steamId)).FullName;

        var profile = new ProfileModel
        {
            UserId = userId ?? steamId,
            NextChrSlot = 99,
            MetaResources = [new MetaResource { MetaRow = "Credits", Count = 10 }],
        };
        var roster = Enumerable.Range(1, characters)
            .Select(slot => new CharacterModel { CharacterName = $"Char{slot}", ChrSlot = slot, XP = 100, XP_Debt = 50, IsDead = true })
            .ToList();

        File.WriteAllText(Path.Combine(dir, LazyMaxApplyService.ProfileFile), ProfileSerializer.Serialize(profile));
        File.WriteAllText(Path.Combine(dir, LazyMaxApplyService.CharactersFile), CharactersSerializer.Serialize(roster));
        File.WriteAllText(Path.Combine(dir, LazyMaxApplyService.AccoladesFile), AccoladesSerializer.Serialize(new AccoladesModel()));
        File.WriteAllText(Path.Combine(dir, LazyMaxApplyService.BestiaryFile), BestiarySerializer.Serialize(new BestiaryModel()));
        return dir;
    }

    // ---- Preview ----------------------------------------------------------

    [Fact]
    public async Task PreviewAsync_BuildsApplicablePlan_WithCountsAndFourFilesInRecoveryOrder()
    {
        var dir = CreateSave();

        var plan = await NewService(new FakeSafeSaveWriter()).PreviewAsync(dir);

        plan.CanApply.Should().BeTrue();
        plan.Validation.HasErrors.Should().BeFalse();
        plan.Files.Select(f => f.FileName).Should().ContainInOrder(
            LazyMaxApplyService.ProfileFile,
            LazyMaxApplyService.CharactersFile,
            LazyMaxApplyService.AccoladesFile,
            LazyMaxApplyService.BestiaryFile);
        plan.Files.Should().OnlyContain(f => f.NewContent.Length > 0);

        plan.Result!.CharactersMaxed.Should().Be(2);
        plan.Result.AccoladesAdded.Should().BeGreaterThan(200);
        plan.Result.BestiaryGroupsTotal.Should().BeGreaterThan(70);
        plan.Result.WorkshopUnlocksTotal.Should().BeGreaterThan(300);
    }

    [Fact]
    public async Task PreviewAsync_MissingFile_CannotApply()
    {
        var dir = CreateSave();
        File.Delete(Path.Combine(dir, LazyMaxApplyService.AccoladesFile));

        var plan = await NewService(new FakeSafeSaveWriter()).PreviewAsync(dir);

        plan.CanApply.Should().BeFalse();
        plan.Files.Should().BeEmpty();
        plan.Validation.Errors.Should().Contain(i => i.Code == "file-missing");
    }

    [Fact]
    public async Task PreviewAsync_ProfileUserIdMismatch_CannotApply()
    {
        var dir = CreateSave(userId: "99999999999999999");

        var plan = await NewService(new FakeSafeSaveWriter()).PreviewAsync(dir);

        plan.CanApply.Should().BeFalse();
        plan.Validation.Errors.Should().Contain(i => i.Code == "profile-userid-mismatch");
    }

    // ---- Apply (fake writer) ---------------------------------------------

    [Fact]
    public async Task ApplyAsync_WhenCannotApply_WritesNothing()
    {
        var dir = CreateSave(userId: "99999999999999999");
        var writer = new FakeSafeSaveWriter();
        var service = NewService(writer);
        var plan = await service.PreviewAsync(dir);

        var report = await service.ApplyAsync(plan);

        report.Applied.Should().BeFalse();
        report.FileResults.Should().BeEmpty();
        writer.WrittenPaths.Should().BeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_WritesAllFilesInRecoveryOrder()
    {
        var dir = CreateSave();
        var writer = new FakeSafeSaveWriter();
        var service = NewService(writer);
        var plan = await service.PreviewAsync(dir);

        var report = await service.ApplyAsync(plan);

        report.Applied.Should().BeTrue();
        report.FileResults.Should().HaveCount(4).And.OnlyContain(r => r.Ok);
        writer.WrittenPaths.Select(Path.GetFileName).Should().ContainInOrder(
            LazyMaxApplyService.ProfileFile,
            LazyMaxApplyService.CharactersFile,
            LazyMaxApplyService.AccoladesFile,
            LazyMaxApplyService.BestiaryFile);
    }

    [Fact]
    public async Task ApplyAsync_StopsAtFirstFailure_AndDoesNotWriteLaterFiles()
    {
        var dir = CreateSave();
        var writer = new FakeSafeSaveWriter(failOnFileNameContaining: LazyMaxApplyService.AccoladesFile);
        var service = NewService(writer);
        var plan = await service.PreviewAsync(dir);

        var report = await service.ApplyAsync(plan);

        report.Applied.Should().BeFalse();
        report.FileResults.Should().HaveCount(3, "Profile + Characters succeed, Accolades fails, Bestiary is never attempted");
        report.FileResults[^1].Ok.Should().BeFalse();
        writer.WrittenPaths.Select(Path.GetFileName).Should().ContainInOrder(
            LazyMaxApplyService.ProfileFile, LazyMaxApplyService.CharactersFile);
        writer.WrittenPaths.Should().NotContain(p => p.EndsWith(LazyMaxApplyService.BestiaryFile, StringComparison.Ordinal));
    }

    // ---- Apply (real SafeSaveWriter — end to end) ------------------------

    [Fact]
    public async Task ApplyAsync_RealWriter_MaxesSaveOnDisk_AndBacksUpEachFile()
    {
        var dir = CreateSave();
        var writer = new SafeSaveWriter(new BackupManager(FixedClock.Default), new SystemGuidProvider());
        var service = NewService(writer);

        var plan = await service.PreviewAsync(dir);
        var report = await service.ApplyAsync(plan);

        report.Applied.Should().BeTrue();
        report.FileResults.Should().HaveCount(4).And.OnlyContain(r => r.Ok && r.BackupPath != null);

        // Each original was backed up before being overwritten (CONSTITUTION III).
        Directory.GetFiles(dir, "*" + BackupManager.BackupInfix + "*").Should().HaveCountGreaterThanOrEqualTo(4);

        // The files on disk now re-parse to the maxed state.
        var profile = ProfileParser.Parse(await File.ReadAllTextAsync(Path.Combine(dir, LazyMaxApplyService.ProfileFile)));
        profile.MetaResources.Should().Contain(m => m.MetaRow == "Credits" && m.Count >= LazyMaxService.MaxedMetaResourceCount);
        profile.Talents.Should().Contain(t => t.RowName.StartsWith("Workshop_", StringComparison.Ordinal) && t.Rank == LazyMaxService.WorkshopUnlockRank);

        var characters = CharactersParser.Parse(await File.ReadAllTextAsync(Path.Combine(dir, LazyMaxApplyService.CharactersFile)));
        characters.Should().OnlyContain(c => c.XP >= LazyMaxService.MinMaxedExperience && c.XP_Debt == 0 && !c.IsDead);

        var accolades = AccoladesParser.Parse(await File.ReadAllTextAsync(Path.Combine(dir, LazyMaxApplyService.AccoladesFile)));
        accolades.CompletedAccolades.Should().HaveCountGreaterThan(200);

        var bestiary = BestiaryParser.Parse(await File.ReadAllTextAsync(Path.Combine(dir, LazyMaxApplyService.BestiaryFile)));
        bestiary.BestiaryTracking.Should().HaveCountGreaterThan(70);
    }

    public void Dispose() => _temp.Dispose();
}

using System.IO.Compression;
using FluentAssertions;
using IUUT.Core.Abstractions;
using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;
using IUUT.Core.Recovery;
using IUUT.Core.Serializers;
using IUUT.Core.Services;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class RecoveryServiceTests : IDisposable
{
    private const string SteamId = "11111111111111111";

    private readonly TempDir _temp = new();
    private readonly string _profileDir;
    private readonly string _backupDir;
    private readonly RecoveryPlanner _planner =
        new(new HealthScanService(), new BackupChainWalker(), new TemplateRepairService());
    private readonly RecoveryService _service;

    public RecoveryServiceTests()
    {
        _profileDir = Directory.CreateDirectory(Path.Combine(_temp.Path, SteamId)).FullName;
        _backupDir = Path.Combine(_temp.Path, "iuut-backups");
        _service = new RecoveryService(
            new SafeSaveWriter(new BackupManager(FixedClock.Default), new SystemGuidProvider()),
            FixedClock.Default);
    }

    private void Write(string name, string content) => File.WriteAllText(Path.Combine(_profileDir, name), content);

    private string ReadFile(string name) => File.ReadAllText(Path.Combine(_profileDir, name));

    private static RecoveryFileResult Result(RecoveryReport report, string name) =>
        report.Files.Single(f => f.RelativePath == name);

    [Fact]
    public async Task ExecuteAsync_TakesMasterBackup_RestoresTemplatesAndReportsFailures()
    {
        Write("Profile.json", "{ broken");
        Write("Profile.json.backup", ProfileSerializer.Serialize(new ProfileModel { UserId = SteamId }));
        Write("Characters.json", "not json");          // modeled, no backup → template
        Write("MetaInventory.json", "{ broken");        // unmodeled, no backup → unrecoverable
        Write("Accolades.json", AccoladesSerializer.Serialize(new AccoladesModel())); // healthy

        var plan = _planner.Plan(_profileDir);
        var report = await _service.ExecuteAsync(plan, _backupDir);

        // Master backup zip taken, under the backups dir, capturing the PRE-recovery (corrupt) state.
        report.MasterBackupZipPath.Should().NotBeNull().And.StartWith(_backupDir);
        File.Exists(report.MasterBackupZipPath!).Should().BeTrue();
        using (var zip = ZipFile.OpenRead(report.MasterBackupZipPath!))
        {
            using var reader = new StreamReader(zip.GetEntry("Profile.json")!.Open());
            reader.ReadToEnd().Should().Be("{ broken", "the zip preserves the originals before recovery overwrote them");
        }

        // Profile restored from its clean backup.
        Result(report, "Profile.json").Outcome.Should().Be(RecoveryOutcome.RestoreFromGameBackup);
        Result(report, "Profile.json").Changed.Should().BeTrue();
        ProfileParser.Parse(ReadFile("Profile.json")).UserId.Should().Be(SteamId);

        // Characters template-repaired to a valid empty roster.
        Result(report, "Characters.json").Outcome.Should().Be(RecoveryOutcome.TemplateRepair);
        CharactersParser.Parse(ReadFile("Characters.json")).Should().BeEmpty();

        // MetaInventory could not be recovered → left untouched, marked failed.
        Result(report, "MetaInventory.json").Failed.Should().BeTrue();
        ReadFile("MetaInventory.json").Should().Be("{ broken");

        // Healthy file untouched.
        Result(report, "Accolades.json").Changed.Should().BeFalse();
        Result(report, "Accolades.json").Failed.Should().BeFalse();

        // Overall.
        report.ChangedCount.Should().Be(2);
        report.FailedCount.Should().Be(1);
        report.PartialRecovery.Should().BeTrue();
        report.Succeeded.Should().BeFalse();

        // Each overwritten file kept its own pre-write backup (CONSTITUTION III).
        Result(report, "Profile.json").BackupPath.Should().NotBeNull();
        File.Exists(Result(report, "Profile.json").BackupPath!).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_AllClean_TakesBackup_ButChangesNothing()
    {
        Write("Profile.json", ProfileSerializer.Serialize(new ProfileModel { UserId = SteamId }));
        Write("Characters.json", CharactersSerializer.Serialize(new List<CharacterModel>()));

        var plan = _planner.Plan(_profileDir);
        var report = await _service.ExecuteAsync(plan, _backupDir);

        File.Exists(report.MasterBackupZipPath!).Should().BeTrue();
        report.ChangedCount.Should().Be(0);
        report.FailedCount.Should().Be(0);
        report.Succeeded.Should().BeTrue();
        report.PartialRecovery.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_DestinationInsideProfileFolder_Throws()
    {
        Write("Profile.json", ProfileSerializer.Serialize(new ProfileModel { UserId = SteamId }));
        var plan = _planner.Plan(_profileDir);

        var act = async () => await _service.ExecuteAsync(plan, Path.Combine(_profileDir, "backups"));

        await act.Should().ThrowAsync<ArgumentException>("the master backup must not live inside the folder it snapshots");
    }

    [Fact]
    public async Task ExecuteAsync_MasterBackup_ExcludesIuutTempArtifacts()
    {
        Write("Profile.json", "{ broken");
        Write("Profile.json.backup", ProfileSerializer.Serialize(new ProfileModel { UserId = SteamId }));
        Write("Profile.json.iuut-tmp-deadbeef", "leftover temp content");

        var plan = _planner.Plan(_profileDir);
        var report = await _service.ExecuteAsync(plan, _backupDir);

        using var zip = ZipFile.OpenRead(report.MasterBackupZipPath!);
        zip.Entries.Should().NotContain(e => e.FullName.Contains(".iuut-tmp-"), "transient temp files are not part of the snapshot");
        zip.Entries.Should().Contain(e => e.FullName == "Profile.json", "the corrupt save itself is captured");
    }

    public void Dispose() => _temp.Dispose();
}

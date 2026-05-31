using System.Text.Json;
using FluentAssertions;
using IUUT.Core.Recovery;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Integration;

public sealed class BackupChainWalkerTests : IDisposable
{
    private static readonly DateTime _base = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly TempDir _temp = new();
    private readonly BackupChainWalker _walker = new();

    private static bool ParsesJson(string content)
    {
        using var _ = JsonDocument.Parse(content);
        return true;
    }

    private string Write(string relativeName, string content, int hourOffset)
    {
        var path = Path.Combine(_temp.Path, relativeName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        File.SetLastWriteTimeUtc(path, _base.AddHours(hourOffset));
        return path;
    }

    [Fact]
    public void Scan_PicksNewestCleanGameBackup_SkippingCorruptNewer()
    {
        var file = Write("Profile.json", "{ corrupt", 0);
        Write("Profile.json.backup", "{\"a\":1}", 1);          // clean, older
        var newestClean = Write("Profile.json.backup_1", "{\"a\":2}", 3); // clean, newest clean
        Write("Profile.json.backup_2", "{ broken", 5);          // newest, but corrupt

        var result = _walker.Scan(file, ParsesJson);

        result.HasCleanCandidate.Should().BeTrue();
        result.ChosenKind.Should().Be(BackupRestoreKind.GameBackup);
        result.Chosen!.Path.Should().Be(newestClean);
        result.Candidates.Should().HaveCount(3);
    }

    [Fact]
    public void Scan_Prospect_WithTwoOrMoreClean_PrefersSecondNewest()
    {
        var file = Write("world.json", "{ corrupt", 0);
        Write("world.json.backup", "{\"x\":1}", 1);              // oldest clean
        var secondNewest = Write("world.json.backup_1", "{\"x\":2}", 2);
        Write("world.json.backup_2", "{\"x\":3}", 3);            // newest clean (the suspect flush)

        var result = _walker.Scan(file, ParsesJson, isProspect: true);

        result.Chosen!.Path.Should().Be(secondNewest, "the freshest prospect backup may be the corrupted flush");
    }

    [Fact]
    public void Scan_NonProspect_AlwaysPicksNewestClean()
    {
        var file = Write("Characters.json", "{ corrupt", 0);
        Write("Characters.json.backup", "{\"x\":1}", 1);
        var newest = Write("Characters.json.backup_1", "{\"x\":2}", 2);

        var result = _walker.Scan(file, ParsesJson, isProspect: false);

        result.Chosen!.Path.Should().Be(newest);
    }

    [Fact]
    public void Scan_NoGameBackup_FallsBackToNewestCleanIuutBackup()
    {
        var file = Write("MetaInventory.json", "{ corrupt", 0);
        Write("MetaInventory.json.iuut-backup-20260101-000000", "{\"old\":1}", 1);
        var newestIuut = Write("MetaInventory.json.iuut-backup-20260102-000000", "{\"new\":1}", 4);

        var result = _walker.Scan(file, ParsesJson);

        result.ChosenKind.Should().Be(BackupRestoreKind.IuutBackup);
        result.Chosen!.Path.Should().Be(newestIuut);
    }

    [Fact]
    public void Scan_PrefersGameBackupOverIuut_WhenBothClean()
    {
        var file = Write("Profile.json", "{ corrupt", 0);
        var game = Write("Profile.json.backup", "{\"g\":1}", 1);
        Write("Profile.json.iuut-backup-20260109-000000", "{\"i\":1}", 9); // newer, but IUUT

        var result = _walker.Scan(file, ParsesJson);

        result.ChosenKind.Should().Be(BackupRestoreKind.GameBackup);
        result.Chosen!.Path.Should().Be(game, "game-rotated backups outrank IUUT fallbacks");
    }

    [Fact]
    public void Scan_DiscoversLoadoutsDotNumberDotBackupConvention()
    {
        var file = Write(Path.Combine("Loadout", "Loadouts.json"), "{ corrupt", 0);
        var oddName = Write(Path.Combine("Loadout", "Loadouts.json.4.backup"), "{\"l\":1}", 1);

        var result = _walker.Scan(file, ParsesJson);

        result.Chosen!.Path.Should().Be(oddName, "the glob must catch the .<N>.backup naming, not just .backup_N");
    }

    [Fact]
    public void Scan_NoCleanCandidate_ReturnsNone()
    {
        var file = Write("Mounts.json", "{ corrupt", 0);
        Write("Mounts.json.backup", "{ also broken", 1);

        var result = _walker.Scan(file, ParsesJson);

        result.HasCleanCandidate.Should().BeFalse();
        result.Chosen.Should().BeNull();
        result.ChosenKind.Should().Be(BackupRestoreKind.None);
        result.Candidates.Should().ContainSingle();
    }

    [Fact]
    public void Scan_EmptyOrWhitespaceBackup_IsNeverChosen()
    {
        var file = Write("Profile.json", "{ corrupt", 0);
        Write("Profile.json.backup", "", 5);                          // empty, newest
        var valid = Write("Profile.json.backup_1", "{\"a\":1}", 1);   // valid, older

        var result = _walker.Scan(file, ParsesJson);

        result.Chosen!.Path.Should().Be(valid, "a zero-byte backup is not a valid restore source");
    }

    [Fact]
    public void Scan_EqualMtimes_PicksDeterministically()
    {
        var file = Write("Mounts.json", "{ corrupt", 0);
        var a = Write("Mounts.json.backup", "{\"x\":1}", 3);
        Write("Mounts.json.backup_1", "{\"x\":2}", 3);                // identical mtime

        var first = _walker.Scan(file, ParsesJson).Chosen!.Path;
        var second = _walker.Scan(file, ParsesJson).Chosen!.Path;

        first.Should().Be(second, "selection must be stable across runs");
        first.Should().Be(a, "ties break by ordinal path; 'Mounts.json.backup' sorts before '.backup_1'");
    }

    public void Dispose() => _temp.Dispose();
}

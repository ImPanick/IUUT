using FluentAssertions;
using IUUT.Core.Io;
using IUUT.Core.Tests.TestDoubles;
using Xunit;

namespace IUUT.Core.Tests.Unit;

/// <summary>
/// Verifies the Backup Manager engine: inventory (newest first, infix-only), reversible
/// restore (current file backed up first), and keep-newest-N pruning.
/// </summary>
public class BackupInventoryServiceTests
{
    private static BackupInventoryService Service() =>
        new(new BackupManager(new FixedClock(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero))));

    private static string Write(TempDir dir, string relativePath, string content, DateTime lastWriteUtc)
    {
        var path = Path.Combine(dir.Path, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
        return path;
    }

    [Fact]
    public void List_FindsOnlyIuutBackups_NewestFirst()
    {
        using var dir = new TempDir();
        Write(dir, "Profile.json", "{}", new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));
        var older = Write(dir, "Profile.json.iuut-backup-20260101-000000", "{}", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = Write(dir, "Profile.json.iuut-backup-20260103-000000", "{}", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        Write(dir, Path.Combine("Prospects", "Olympus.json.iuut-backup-20260102-000000"), "{}", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        var entries = BackupInventoryService.List(dir.Path);

        entries.Should().HaveCount(3, "the original file itself is not a backup");
        entries[0].BackupPath.Should().Be(newer);
        entries[2].BackupPath.Should().Be(older);
        entries[0].OriginalName.Should().Be("Profile.json");
        entries.Should().Contain(e => e.OriginalName == "Olympus.json", "backups in subfolders are inventoried");
    }

    [Fact]
    public void Restore_ReplacesOriginal_AndBacksUpTheCurrentFileFirst()
    {
        using var dir = new TempDir();
        Write(dir, "Profile.json", "current", new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));
        Write(dir, "Profile.json.iuut-backup-20260101-000000", "older-good", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var service = Service();
        var entry = BackupInventoryService.List(dir.Path).Single();

        var result = service.Restore(entry);

        result.Ok.Should().BeTrue(result.Error);
        File.ReadAllText(entry.OriginalPath).Should().Be("older-good");
        result.PreRestoreBackupPath.Should().NotBeNull("the current file must be backed up before a restore");
        File.ReadAllText(result.PreRestoreBackupPath!).Should().Be("current");
    }

    [Fact]
    public void Prune_KeepsTheNewestNPerOriginal()
    {
        using var dir = new TempDir();
        for (var day = 1; day <= 5; day++)
        {
            Write(dir, $"Profile.json.iuut-backup-2026010{day}-000000", "{}", new DateTime(2026, 1, day, 0, 0, 0, DateTimeKind.Utc));
        }

        Write(dir, "Characters.json.iuut-backup-20260101-000000", "{}", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var deleted = BackupInventoryService.Prune(dir.Path, keepPerFile: 2);

        deleted.Should().Be(3, "five Profile backups minus the two newest; the single Characters backup survives");
        var remaining = BackupInventoryService.List(dir.Path);
        remaining.Should().HaveCount(3);
        remaining.Where(e => e.OriginalName == "Profile.json")
            .Select(e => e.TakenUtc.Day).Should().BeEquivalentTo([5, 4], "the newest two remain");
    }
}

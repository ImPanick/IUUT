namespace IUUT.Core.Io;

/// <summary>
/// The Backup Manager's engine (roadmap Tier 2): inventories IUUT's own timestamped backups
/// (<c>&lt;File&gt;.iuut-backup-&lt;stamp&gt;</c>) across a save folder, restores one (backing up
/// the CURRENT file first, so a restore is itself reversible), and prunes old backups keeping
/// the newest N per original file. Only files carrying the IUUT backup infix are ever touched.
/// </summary>
public sealed class BackupInventoryService
{
    private readonly BackupManager _backups;

    /// <summary>Creates the service over the backup creator (used to back up before a restore).</summary>
    public BackupInventoryService(BackupManager backups)
    {
        ArgumentNullException.ThrowIfNull(backups);
        _backups = backups;
    }

    /// <summary>One backup on disk and the original file it belongs to.</summary>
    public sealed record BackupEntry(
        string BackupPath,
        string OriginalPath,
        string OriginalName,
        DateTimeOffset TakenUtc,
        long SizeBytes);

    /// <summary>The outcome of a restore: success, the pre-restore backup of the current file, or the error.</summary>
    public sealed record RestoreResult(bool Ok, string? PreRestoreBackupPath, string? Error);

    /// <summary>
    /// Every IUUT backup under <paramref name="saveFolder"/> (recursive), newest first.
    /// Unreadable entries are skipped.
    /// </summary>
    public static IReadOnlyList<BackupEntry> List(string saveFolder)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveFolder);
        if (!Directory.Exists(saveFolder))
        {
            return [];
        }

        var entries = new List<BackupEntry>();
        foreach (var path in Directory.EnumerateFiles(saveFolder, "*" + BackupManager.BackupInfix + "*", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(path);
            var infixAt = fileName.IndexOf(BackupManager.BackupInfix, StringComparison.Ordinal);
            if (infixAt <= 0)
            {
                continue;
            }

            try
            {
                var info = new FileInfo(path);
                var originalName = fileName[..infixAt];
                entries.Add(new BackupEntry(
                    path,
                    Path.Combine(Path.GetDirectoryName(path)!, originalName),
                    originalName,
                    new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
                    info.Length));
            }
            catch (IOException)
            {
                // skip an unreadable entry rather than failing the whole inventory
            }
        }

        return entries.OrderByDescending(e => e.TakenUtc).ToList();
    }

    /// <summary>
    /// Restores <paramref name="entry"/> over its original file. The current original (when present)
    /// is backed up first, and the copy lands via temp + atomic move — a restore never destroys state.
    /// </summary>
    public RestoreResult Restore(BackupEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            if (!File.Exists(entry.BackupPath))
            {
                return new RestoreResult(false, null, $"Backup no longer exists: {entry.BackupPath}");
            }

            string? preRestore = null;
            if (File.Exists(entry.OriginalPath))
            {
                preRestore = _backups.CreateBackup(entry.OriginalPath);
            }

            var temp = entry.OriginalPath + ".iuut-restore-tmp";
            File.Copy(entry.BackupPath, temp, overwrite: true);
            File.Move(temp, entry.OriginalPath, overwrite: true);
            return new RestoreResult(true, preRestore, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new RestoreResult(false, null, ex.Message);
        }
    }

    /// <summary>
    /// Deletes old backups under <paramref name="saveFolder"/>, keeping the newest
    /// <paramref name="keepPerFile"/> per original file. Returns how many were deleted;
    /// entries that fail to delete are left in place and not counted.
    /// </summary>
    public static int Prune(string saveFolder, int keepPerFile)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(keepPerFile);

        var deleted = 0;
        foreach (var group in List(saveFolder).GroupBy(e => e.OriginalPath, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var entry in group.OrderByDescending(e => e.TakenUtc).Skip(keepPerFile))
            {
                try
                {
                    File.Delete(entry.BackupPath);
                    deleted++;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // leave it; the next prune can retry
                }
            }
        }

        return deleted;
    }
}

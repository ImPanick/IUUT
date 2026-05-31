using IUUT.Core.Io;

namespace IUUT.Core.Recovery;

/// <summary>
/// Walks a save file's backup chain to pick the best clean restore source (master doc §12.1,
/// §7.6). Globs <c>&lt;File&gt;.*backup*</c> siblings — **not** a fixed name list — so every
/// game convention is caught (<c>.backup</c>, <c>.backup_N</c>, and Loadouts' <c>.&lt;N&gt;.backup</c>).
/// Ranks by clean-parse then mtime; for prospects with ≥2 clean candidates it prefers the
/// <b>second-newest</b> (the freshest may be the corrupted in-memory flush). Falls back to
/// IUUT's own <c>.iuut-backup-*</c> copies for files the game never rotates.
/// </summary>
public sealed class BackupChainWalker
{
    /// <summary>
    /// Walks the chain for <paramref name="filePath"/>. <paramref name="isClean"/> validates a
    /// candidate's content (parse, and for prospects SHA-1); any throw is treated as not-clean.
    /// Set <paramref name="isProspect"/> to apply the second-newest prospect rule.
    /// </summary>
    public BackupScanResult Scan(string filePath, Func<string, bool> isClean, bool isProspect = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentNullException.ThrowIfNull(isClean);

        var directory = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);
        var candidates = new List<BackupCandidate>();

        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            foreach (var path in EnumerateBackups(directory, fileName))
            {
                var isIuut = Path.GetFileName(path).Contains(BackupManager.BackupInfix, StringComparison.Ordinal);
                // An empty or whitespace-only backup is never a valid restore source.
                var clean = TryRead(path, out var content)
                    && !string.IsNullOrWhiteSpace(content)
                    && IsCleanSafe(isClean, content);
                candidates.Add(new BackupCandidate
                {
                    Path = path,
                    LastModifiedUtc = GetLastWriteUtc(path),
                    IsClean = clean,
                    IsIuutBackup = isIuut,
                });
            }
        }

        var (chosen, kind) = Choose(candidates, isProspect);
        return new BackupScanResult
        {
            FilePath = filePath,
            Candidates = candidates,
            Chosen = chosen,
            ChosenKind = kind,
        };
    }

    private static (BackupCandidate? Chosen, BackupRestoreKind Kind) Choose(
        IReadOnlyList<BackupCandidate> all, bool isProspect)
    {
        var gameClean = all
            .Where(c => !c.IsIuutBackup && c.IsClean)
            .OrderByDescending(c => c.LastModifiedUtc)
            .ThenBy(c => c.Path, StringComparer.Ordinal) // stable tiebreak when mtimes are equal
            .ToList();

        if (gameClean.Count > 0)
        {
            // Prospect override: the freshest backup may be the corrupted in-memory flush.
            var pick = isProspect && gameClean.Count >= 2 ? gameClean[1] : gameClean[0];
            return (pick, BackupRestoreKind.GameBackup);
        }

        var iuutClean = all
            .Where(c => c.IsIuutBackup && c.IsClean)
            .OrderByDescending(c => c.LastModifiedUtc)
            .ThenBy(c => c.Path, StringComparer.Ordinal) // stable tiebreak when mtimes are equal
            .ToList();

        return iuutClean.Count > 0
            ? (iuutClean[0], BackupRestoreKind.IuutBackup)
            : (null, BackupRestoreKind.None);
    }

    private static IEnumerable<string> EnumerateBackups(string directory, string fileName)
    {
        var prefix = fileName + ".";
        foreach (var path in Directory.EnumerateFiles(directory))
        {
            var name = Path.GetFileName(path);
            if (name.StartsWith(prefix, StringComparison.Ordinal) &&
                name.Contains("backup", StringComparison.OrdinalIgnoreCase))
            {
                yield return path;
            }
        }
    }

    private static bool IsCleanSafe(Func<string, bool> isClean, string content)
    {
#pragma warning disable CA1031 // A candidate that fails validation for ANY reason is simply "not clean".
        try
        {
            return isClean(content);
        }
        catch (Exception)
        {
            return false;
        }
#pragma warning restore CA1031
    }

    private static bool TryRead(string path, out string content)
    {
        try
        {
            content = File.ReadAllText(path);
            return true;
        }
        catch (IOException)
        {
            content = "";
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            content = "";
            return false;
        }
    }

    private static DateTimeOffset GetLastWriteUtc(string path)
    {
        try
        {
            return new DateTimeOffset(File.GetLastWriteTimeUtc(path), TimeSpan.Zero);
        }
        catch (IOException)
        {
            return DateTimeOffset.MinValue;
        }
        catch (UnauthorizedAccessException)
        {
            return DateTimeOffset.MinValue;
        }
    }
}

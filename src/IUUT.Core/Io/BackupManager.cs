using System.Globalization;
using IUUT.Core.Abstractions;

namespace IUUT.Core.Io;

/// <summary>
/// Creates IUUT's own timestamped backups before any save-file write
/// (CONSTITUTION III). Backup name format: <c>&lt;File&gt;.iuut-backup-&lt;yyyyMMdd-HHmmss&gt;</c>,
/// disambiguated with a <c>-N</c> suffix if several are taken within the same second.
/// </summary>
public sealed class BackupManager
{
    /// <summary>The fixed infix that marks an IUUT-created backup.</summary>
    public const string BackupInfix = ".iuut-backup-";

    private readonly IClock _clock;

    /// <summary>Creates a <see cref="BackupManager"/> using the given clock.</summary>
    public BackupManager(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
    }

    /// <summary>
    /// Copies <paramref name="filePath"/> to a timestamped backup alongside it and
    /// returns the backup path. The original is left untouched.
    /// </summary>
    /// <exception cref="FileNotFoundException">The source file does not exist.</exception>
    public string CreateBackup(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Cannot back up a file that does not exist.", filePath);
        }

        var stamp = _clock.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var baseName = filePath + BackupInfix + stamp;

        var backupPath = baseName;
        var n = 2;
        while (File.Exists(backupPath))
        {
            backupPath = string.Create(CultureInfo.InvariantCulture, $"{baseName}-{n}");
            n++;
        }

        File.Copy(filePath, backupPath, overwrite: false);
        return backupPath;
    }
}

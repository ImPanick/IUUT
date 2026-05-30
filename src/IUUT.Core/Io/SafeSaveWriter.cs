using System.Text;

namespace IUUT.Core.Io;

/// <summary>
/// Implements the mandatory save-file write protocol (CONSTITUTION III):
/// <c>backup → write (UTF-8 without BOM) → re-read → validate → restore-on-failure</c>.
/// No save-file mutator may write to disk except through this type.
/// </summary>
public sealed class SafeSaveWriter
{
    // UTF-8 WITHOUT BOM — the game writes no BOM and a BOM is a deviation
    // (Icarus-Analysis §10 rule 4, master doc §7.7). The shared framework UTF-8
    // singleton emits a BOM, so we always construct our own no-BOM encoder here.
    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly BackupManager _backups;

    /// <summary>Creates a <see cref="SafeSaveWriter"/> using the given backup manager.</summary>
    public SafeSaveWriter(BackupManager backups)
    {
        ArgumentNullException.ThrowIfNull(backups);
        _backups = backups;
    }

    /// <summary>
    /// Safely writes <paramref name="newContent"/> to <paramref name="filePath"/>:
    /// backs up an existing file, writes UTF-8 without BOM, re-reads, and runs
    /// <paramref name="validate"/> over the re-read content. If validation throws (or
    /// any write/read fails), the original file is restored from the backup (or the
    /// newly-created file is deleted) and the failure is returned in the result.
    /// </summary>
    /// <param name="filePath">Absolute path to the save file.</param>
    /// <param name="newContent">The new file content.</param>
    /// <param name="validate">
    /// Re-parse/validation callback invoked with the content read back from disk.
    /// Throw to signal the write is bad and must be rolled back.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<SafeSaveResult> WriteAsync(
        string filePath,
        string newContent,
        Action<string> validate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentNullException.ThrowIfNull(newContent);
        ArgumentNullException.ThrowIfNull(validate);

        var existed = File.Exists(filePath);
        var backupPath = existed ? _backups.CreateBackup(filePath) : null;

#pragma warning disable CA1031 // Restore-on-ANY-failure is the documented contract (CONSTITUTION III).
        try
        {
            await File.WriteAllTextAsync(filePath, newContent, _utf8NoBom, cancellationToken)
                .ConfigureAwait(false);

            var readBack = await File.ReadAllTextAsync(filePath, _utf8NoBom, cancellationToken)
                .ConfigureAwait(false);

            validate(readBack);

            return SafeSaveResult.Success(filePath, backupPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Restore(filePath, backupPath, existed);
            return SafeSaveResult.Failure(filePath, backupPath, ex);
        }
#pragma warning restore CA1031
    }

    private static void Restore(string filePath, string? backupPath, bool existed)
    {
        if (backupPath is not null)
        {
            File.Copy(backupPath, filePath, overwrite: true);
        }
        else if (!existed && File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}

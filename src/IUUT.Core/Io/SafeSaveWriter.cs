using System.Text;
using IUUT.Core.Abstractions;

namespace IUUT.Core.Io;

/// <summary>
/// Implements the mandatory save-file write protocol (CONSTITUTION III; CODE_STYLE §10):
/// <c>backup → write temp (UTF-8 without BOM) → re-read + validate → atomic rename</c>.
/// The validated temp file is renamed over the original only after it passes, so the
/// live save file can never contain unvalidated or partially-written content. No
/// save-file mutator may write to disk except through this type.
/// </summary>
public sealed class SafeSaveWriter : ISafeSaveWriter
{
    // UTF-8 WITHOUT BOM — the game writes no BOM and a BOM is a deviation
    // (Icarus-Analysis §10 rule 4, master doc §7.7). The shared framework UTF-8
    // singleton emits a BOM, so we always construct our own no-BOM encoder here.
    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly BackupManager _backups;
    private readonly IGuidProvider _guids;

    /// <summary>Creates a <see cref="SafeSaveWriter"/> using the given backup manager and GUID source.</summary>
    public SafeSaveWriter(BackupManager backups, IGuidProvider guids)
    {
        ArgumentNullException.ThrowIfNull(backups);
        ArgumentNullException.ThrowIfNull(guids);
        _backups = backups;
        _guids = guids;
    }

    /// <summary>
    /// Safely writes <paramref name="newContent"/> to <paramref name="filePath"/>:
    /// backs up an existing file, writes the content to a temp sibling
    /// (<c>&lt;File&gt;.iuut-tmp-&lt;guid&gt;</c>) as UTF-8 without BOM, re-reads it and runs
    /// <paramref name="validate"/> over it, and only then atomically renames the temp
    /// over the original. If anything fails (or <paramref name="validate"/> throws) the
    /// temp is removed and the original is left exactly as it was — the live file is
    /// never replaced by unvalidated content.
    /// </summary>
    /// <param name="filePath">Absolute path to the save file.</param>
    /// <param name="newContent">The new file content.</param>
    /// <param name="validate">
    /// Re-parse/validation callback invoked with the content read back from the temp file.
    /// Throw to signal the content is bad and the original must be kept.
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
        // CONSTITUTION III: back up the existing file before any write operation.
        var backupPath = existed ? _backups.CreateBackup(filePath) : null;
        var tempPath = filePath + ".iuut-tmp-" + _guids.NewGuid().ToString("N");

#pragma warning disable CA1031 // Cleanup/keep-original on ANY failure is the documented contract (CONSTITUTION III).
        try
        {
            await File.WriteAllTextAsync(tempPath, newContent, _utf8NoBom, cancellationToken)
                .ConfigureAwait(false);

            var readBack = await File.ReadAllTextAsync(tempPath, _utf8NoBom, cancellationToken)
                .ConfigureAwait(false);

            // Validate the TEMP file before the original is ever touched.
            validate(readBack);

            // Atomic replace: same-volume sibling rename (NTFS MoveFileEx).
            File.Move(tempPath, filePath, overwrite: true);

            return SafeSaveResult.Success(filePath, backupPath);
        }
        catch (Exception ex)
        {
            TryDelete(tempPath);
            if (ex is OperationCanceledException)
            {
                throw;
            }

            // The original was never modified (validation precedes the rename), so it
            // is already intact; the backup (if any) is retained as the prior snapshot.
            return SafeSaveResult.Failure(filePath, backupPath, ex);
        }
#pragma warning restore CA1031
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort temp cleanup; a locked temp file must not mask the real outcome.
        }
    }
}

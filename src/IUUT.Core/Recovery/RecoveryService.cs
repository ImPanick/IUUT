using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using IUUT.Core.Abstractions;
using IUUT.Core.Io;
using IUUT.Core.Parsers;

namespace IUUT.Core.Recovery;

/// <summary>
/// Executes a <see cref="RecoveryPlan"/> (master doc §12.1, §10.1). Takes a full-folder
/// master backup zip <b>before any write</b> (the ultimate undo — the corrupt originals are
/// preserved even when template repair discards them), then carries out each action in restore
/// order through <see cref="ISafeSaveWriter"/> (so every overwritten file is also individually
/// backed up and the written content is re-parsed). Failures are recorded, not thrown.
/// </summary>
public sealed class RecoveryService
{
    private readonly ISafeSaveWriter _writer;
    private readonly IClock _clock;

    /// <summary>Creates the recovery executor over the save writer and a clock (for the backup-zip stamp).</summary>
    public RecoveryService(ISafeSaveWriter writer, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(clock);
        _writer = writer;
        _clock = clock;
    }

    /// <summary>
    /// Runs <paramref name="plan"/>: zips the whole profile folder into
    /// <paramref name="masterBackupDirectory"/>, then restores / template-repairs each file.
    /// </summary>
    public async Task<RecoveryReport> ExecuteAsync(
        RecoveryPlan plan,
        string masterBackupDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrEmpty(masterBackupDirectory);

        var zipPath = CreateMasterBackup(plan.ProfileFolder, masterBackupDirectory);

        var results = new List<RecoveryFileResult>(plan.Actions.Count);
        foreach (var action in plan.Actions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ApplyAsync(action, cancellationToken).ConfigureAwait(false));
        }

        return new RecoveryReport
        {
            ProfileFolder = plan.ProfileFolder,
            MasterBackupZipPath = zipPath,
            Files = results,
            PartialRecovery = plan.PartialRecovery || results.Any(r => r.Failed),
        };
    }

    private string? CreateMasterBackup(string profileFolder, string destinationDirectory)
    {
        // The zip must live OUTSIDE the folder it snapshots, or it would try to include itself.
        var folderFull = Path.GetFullPath(profileFolder);
        var destFull = Path.GetFullPath(destinationDirectory);
        if (destFull == folderFull ||
            destFull.StartsWith(folderFull + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The master-backup directory must be outside the profile folder.", nameof(destinationDirectory));
        }

        try
        {
            Directory.CreateDirectory(destinationDirectory);
            var folderName = Path.GetFileName(Path.TrimEndingDirectorySeparator(profileFolder));
            var stamp = _clock.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

            var zipPath = Path.Combine(destinationDirectory, $"{folderName}.iuut-recovery-{stamp}.zip");
            var n = 2;
            while (File.Exists(zipPath))
            {
                zipPath = Path.Combine(destinationDirectory, $"{folderName}.iuut-recovery-{stamp}-{n}.zip");
                n++;
            }

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            foreach (var file in Directory.EnumerateFiles(profileFolder, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(file);
                // Never snapshot IUUT's own transient temp files or recovery zips — only the save data.
                if (name.Contains(".iuut-tmp-", StringComparison.Ordinal) ||
                    name.Contains(".iuut-recovery-", StringComparison.Ordinal))
                {
                    continue;
                }

                var entryName = Path.GetRelativePath(profileFolder, file).Replace('\\', '/');
                try
                {
                    archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);
                }
                catch (IOException)
                {
                    // A file locked mid-recovery is skipped rather than aborting the whole snapshot.
                }
            }

            return zipPath;
        }
        catch (IOException)
        {
            return null; // recovery still proceeds; per-file SafeSaveWriter backups remain the safety net
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private async Task<RecoveryFileResult> ApplyAsync(RecoveryFileAction action, CancellationToken cancellationToken)
    {
        switch (action.Outcome)
        {
            case RecoveryOutcome.AlreadyOk:
                return new RecoveryFileResult
                {
                    RelativePath = action.RelativePath,
                    Outcome = action.Outcome,
                    Changed = false,
                    Failed = false,
                    Note = "Already healthy.",
                };

            case RecoveryOutcome.Unrecoverable:
                return Failure(action, action.Note ?? "No clean backup and no safe template.");

            case RecoveryOutcome.RestoreFromGameBackup:
            case RecoveryOutcome.RestoreFromIuutBackup:
                var source = TryReadText(action.SourceBackupPath);
                return source is null
                    ? Failure(action, "The backup source could not be read.")
                    : await WriteAsync(action, source, cancellationToken).ConfigureAwait(false);

            case RecoveryOutcome.TemplateRepair:
                return action.RepairedContent is null
                    ? Failure(action, "No repaired content was produced.")
                    : await WriteAsync(action, action.RepairedContent, cancellationToken).ConfigureAwait(false);

            default:
                return Failure(action, "Unknown recovery outcome.");
        }
    }

    private async Task<RecoveryFileResult> WriteAsync(RecoveryFileAction action, string content, CancellationToken cancellationToken)
    {
        var result = await _writer
            .WriteAsync(action.FullPath, content, ReparseValidator(action.RelativePath), cancellationToken)
            .ConfigureAwait(false);

        return new RecoveryFileResult
        {
            RelativePath = action.RelativePath,
            Outcome = action.Outcome,
            Changed = result.Ok,
            Failed = !result.Ok,
            BackupPath = result.BackupPath,
            Note = action.Note,
            Error = result.Error?.Message,
        };
    }

    private static RecoveryFileResult Failure(RecoveryFileAction action, string error) =>
        new()
        {
            RelativePath = action.RelativePath,
            Outcome = action.Outcome,
            Changed = false,
            Failed = true,
            Note = action.Note,
            Error = error,
        };

    private static Action<string> ReparseValidator(string relativePath)
    {
        var name = Path.GetFileName(relativePath);
        return content => Reparse(name, content);
    }

    // Post-write re-parse: throws if the written content is not valid for its file type.
    private static void Reparse(string name, string content)
    {
        switch (name)
        {
            case "Profile.json":
                _ = ProfileParser.Parse(content);
                break;
            case "Characters.json":
                _ = CharactersParser.Parse(content);
                break;
            case "Accolades.json":
                _ = AccoladesParser.Parse(content);
                break;
            case "BestiaryData.json":
                _ = BestiaryParser.Parse(content);
                break;
            default:
                using (JsonDocument.Parse(content))
                {
                }

                break;
        }
    }

    private static string? TryReadText(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}

namespace IUUT.Core.Recovery;

/// <summary>
/// The result of executing a <see cref="RecoveryPlan"/> (master doc §11.3 F-025, §10.1): the
/// master full-folder backup that was taken first, and the per-file outcomes.
/// </summary>
public sealed record RecoveryReport
{
    /// <summary>The profile folder that was recovered.</summary>
    public required string ProfileFolder { get; init; }

    /// <summary>The full-folder master backup zip taken before any write (the ultimate undo), or <c>null</c> if it could not be created.</summary>
    public required string? MasterBackupZipPath { get; init; }

    /// <summary>Per-file results, in the order they were applied (§12.1 restore order).</summary>
    public required IReadOnlyList<RecoveryFileResult> Files { get; init; }

    /// <summary>True when some files could only be partially recovered or not recovered at all (F-024).</summary>
    public required bool PartialRecovery { get; init; }

    /// <summary>
    /// Human-readable guidance for causes recovery can't fix alone (Appendix E): Steam Cloud
    /// overwrite, cloud-sync conflicted copies, antivirus/CFA blocks, schema incompatibility,
    /// cross-file incoherence. Empty when none apply.
    /// </summary>
    public required IReadOnlyList<string> Advisories { get; init; }

    /// <summary>Files that were restored or template-repaired.</summary>
    public int ChangedCount => Files.Count(f => f.Changed);

    /// <summary>Files that could not be recovered.</summary>
    public int FailedCount => Files.Count(f => f.Failed);

    /// <summary>True when no file failed (some may have been template-repaired).</summary>
    public bool Succeeded => FailedCount == 0;
}

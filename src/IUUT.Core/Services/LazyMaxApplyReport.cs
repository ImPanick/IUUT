using IUUT.Core.Io;

namespace IUUT.Core.Services;

/// <summary>
/// The outcome of applying a <see cref="LazyMaxPlan"/> (master doc §13.3 step 8). Writes
/// happen in recovery order and stop at the first failure; <see cref="FileResults"/> lists
/// every file attempted (each carries its backup path for recovery).
/// </summary>
public sealed record LazyMaxApplyReport
{
    /// <summary>True only if every planned file was written and re-validated successfully.</summary>
    public required bool Applied { get; init; }

    /// <summary>One result per file attempted (in write order); a failure is the last entry.</summary>
    public required IReadOnlyList<SafeSaveResult> FileResults { get; init; }

    /// <summary>A short human-readable summary of the outcome.</summary>
    public string? Message { get; init; }

    /// <summary>The backups created (for recovery), newest-write last.</summary>
    public IEnumerable<SafeSaveResult> Backups => FileResults.Where(r => r.BackupPath is not null);
}

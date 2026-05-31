namespace IUUT.Core.Recovery;

/// <summary>
/// The result of walking a file's backup chain (master doc §12.1): every discovered
/// candidate plus the recommended restore source (if any).
/// </summary>
public sealed record BackupScanResult
{
    /// <summary>The save file the chain was walked for.</summary>
    public required string FilePath { get; init; }

    /// <summary>All discovered backups (game + IUUT), unranked.</summary>
    public required IReadOnlyList<BackupCandidate> Candidates { get; init; }

    /// <summary>The recommended restore source per the §12.1 ranking, or <c>null</c> if none is clean.</summary>
    public BackupCandidate? Chosen { get; init; }

    /// <summary>Which kind <see cref="Chosen"/> is.</summary>
    public BackupRestoreKind ChosenKind { get; init; }

    /// <summary>Whether a clean restore source was found.</summary>
    public bool HasCleanCandidate => Chosen is not null;
}

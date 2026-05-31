namespace IUUT.Core.Recovery;

/// <summary>One backup file discovered alongside a save file (master doc §12.1, §7.6).</summary>
public sealed record BackupCandidate
{
    /// <summary>Absolute path to the backup file.</summary>
    public required string Path { get; init; }

    /// <summary>Last-write time (UTC); the ranking key after cleanliness.</summary>
    public required DateTimeOffset LastModifiedUtc { get; init; }

    /// <summary>Whether the backup's content passed the caller's clean check (parses, and for prospects SHA-1 matches).</summary>
    public required bool IsClean { get; init; }

    /// <summary>True if this is an IUUT <c>.iuut-backup-*</c> copy rather than a game-rotated backup.</summary>
    public required bool IsIuutBackup { get; init; }
}

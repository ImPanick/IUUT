namespace IUUT.Core.Recovery;

/// <summary>The outcome of recovering one file (master doc §11.3 F-025).</summary>
public sealed record RecoveryFileResult
{
    /// <summary>Path relative to the profile folder.</summary>
    public required string RelativePath { get; init; }

    /// <summary>The action that was planned for this file.</summary>
    public required RecoveryOutcome Outcome { get; init; }

    /// <summary>True if a write happened and the re-read content validated (restore or template).</summary>
    public required bool Changed { get; init; }

    /// <summary>True if the file could not be recovered (unrecoverable, or a write/read failure).</summary>
    public required bool Failed { get; init; }

    /// <summary>The pre-write backup of the file that was overwritten, if any.</summary>
    public string? BackupPath { get; init; }

    /// <summary>Human-readable note for the report.</summary>
    public string? Note { get; init; }

    /// <summary>Failure detail when <see cref="Failed"/> is true.</summary>
    public string? Error { get; init; }
}

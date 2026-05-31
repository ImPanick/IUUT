namespace IUUT.Core.Recovery;

/// <summary>The planned recovery action for one save file (master doc §12.1).</summary>
public sealed record RecoveryFileAction
{
    /// <summary>Path relative to the profile folder (e.g. <c>Loadout\Loadouts.json</c>).</summary>
    public required string RelativePath { get; init; }

    /// <summary>Absolute path to the file.</summary>
    public required string FullPath { get; init; }

    /// <summary>What recovery will do.</summary>
    public required RecoveryOutcome Outcome { get; init; }

    /// <summary>For a restore: the clean backup to copy from.</summary>
    public string? SourceBackupPath { get; init; }

    /// <summary>For a template repair: the rebuilt content to write.</summary>
    public string? RepairedContent { get; init; }

    /// <summary>Human-readable explanation for the recovery report.</summary>
    public string? Note { get; init; }
}

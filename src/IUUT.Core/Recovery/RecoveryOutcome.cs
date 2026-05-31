namespace IUUT.Core.Recovery;

/// <summary>What a recovery will do to one file (master doc §11.3 F-025).</summary>
public enum RecoveryOutcome
{
    /// <summary>The file is already healthy — no action.</summary>
    AlreadyOk,

    /// <summary>Restore from a clean game-rotated backup.</summary>
    RestoreFromGameBackup,

    /// <summary>Restore from a clean IUUT <c>.iuut-backup-*</c> copy.</summary>
    RestoreFromIuutBackup,

    /// <summary>No clean backup — rebuild a valid skeleton (data is lost; partial recovery).</summary>
    TemplateRepair,

    /// <summary>No clean backup and no safe template — the file cannot be recovered automatically.</summary>
    Unrecoverable,
}

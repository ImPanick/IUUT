namespace IUUT.Core.Recovery;

/// <summary>Which kind of backup a recovery would restore from (master doc §12.1).</summary>
public enum BackupRestoreKind
{
    /// <summary>No clean backup candidate was found.</summary>
    None,

    /// <summary>A game-rotated backup (<c>&lt;File&gt;.backup</c>, <c>.backup_N</c>, <c>.&lt;N&gt;.backup</c>).</summary>
    GameBackup,

    /// <summary>IUUT's own pre-write backup (<c>.iuut-backup-*</c>) — the only net for files the game never rotates.</summary>
    IuutBackup,
}

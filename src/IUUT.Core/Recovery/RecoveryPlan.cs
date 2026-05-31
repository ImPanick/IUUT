namespace IUUT.Core.Recovery;

/// <summary>
/// The full recovery plan for a profile folder (master doc §12.1): the per-file actions
/// in restore order, plus the partial-recovery signal. Produced by <see cref="RecoveryPlanner"/>;
/// carried out by <c>RecoveryService</c>. Building the plan writes nothing.
/// </summary>
public sealed record RecoveryPlan
{
    /// <summary>The profile folder the plan targets.</summary>
    public required string ProfileFolder { get; init; }

    /// <summary>Per-file actions, already sorted into the §12.1 restore order.</summary>
    public required IReadOnlyList<RecoveryFileAction> Actions { get; init; }

    /// <summary>
    /// True when at least one file could only be template-repaired or not recovered at all —
    /// the loud "only partial recovery possible, use Custom" flag (F-024).
    /// </summary>
    public bool PartialRecovery => Actions.Any(a =>
        a.Outcome is RecoveryOutcome.TemplateRepair or RecoveryOutcome.Unrecoverable);

    /// <summary>Whether anything needs doing (any non-OK action).</summary>
    public bool HasWork => Actions.Any(a => a.Outcome != RecoveryOutcome.AlreadyOk);
}

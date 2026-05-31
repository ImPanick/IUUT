using IUUT.Core.Validation;

namespace IUUT.Core.Services;

/// <summary>
/// The preview of a Lazy Max apply (master doc §13.3, F-034): what would change, the
/// validation outcome, and the exact bytes that would be written. Produced by
/// <see cref="LazyMaxApplyService.PreviewAsync"/>; the UI shows it for confirmation, then
/// passes it to <see cref="LazyMaxApplyService.ApplyAsync"/>. Nothing is written during preview.
/// </summary>
public sealed record LazyMaxPlan
{
    /// <summary>The save profile folder this plan targets.</summary>
    public required string SaveFolder { get; init; }

    /// <summary>The per-file change summary (currencies/talents/accolades/bestiary counts), or <c>null</c> if the save could not be read.</summary>
    public LazyMaxResult? Result { get; init; }

    /// <summary>
    /// The combined validation outcome: read/parse problems plus the
    /// <see cref="ValidationEngine"/> profile + character checks. Errors block the apply;
    /// warnings are surfaced for the user to confirm (master doc §13).
    /// </summary>
    public required ValidationResult Validation { get; init; }

    /// <summary>The files to write, in recovery order (master §12.1): Profile, Characters, Accolades, Bestiary.</summary>
    public required IReadOnlyList<PlannedFileWrite> Files { get; init; }

    /// <summary>Whether the plan is safe to apply: all four files parsed and no validation errors.</summary>
    public required bool CanApply { get; init; }
}

using IUUT.Core.Services;
using IUUT.Core.Validation;

namespace IUUT.Core.Editing;

/// <summary>
/// The preview of a Custom edit (master doc §13.3): exactly the files that changed (with their
/// new serialized content) and the validation outcome. Produced by
/// <see cref="CustomApplyService.PreviewAsync"/>; nothing is written during preview.
/// </summary>
public sealed record SaveEditPlan
{
    /// <summary>The save profile folder this plan targets.</summary>
    public required string SaveFolder { get; init; }

    /// <summary>Only the files whose serialized content differs after the edit (minimal writes), in recovery order.</summary>
    public required IReadOnlyList<PlannedFileWrite> ChangedFiles { get; init; }

    /// <summary>Read/parse issues plus the <see cref="ValidationEngine"/> profile + character checks.</summary>
    public required ValidationResult Validation { get; init; }

    /// <summary>Whether the edit is safe to apply: the save parsed and no validation errors.</summary>
    public required bool CanApply { get; init; }

    /// <summary>Whether the edit changed anything at all.</summary>
    public bool HasChanges => ChangedFiles.Count > 0;
}

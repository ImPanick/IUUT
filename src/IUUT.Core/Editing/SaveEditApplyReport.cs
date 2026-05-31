using IUUT.Core.Io;

namespace IUUT.Core.Editing;

/// <summary>The outcome of applying a <see cref="SaveEditPlan"/> (master doc §13.3 step 8).</summary>
public sealed record SaveEditApplyReport
{
    /// <summary>True only if every changed file was written and re-validated successfully.</summary>
    public required bool Applied { get; init; }

    /// <summary>One result per file written (in order); a failure is the last entry.</summary>
    public required IReadOnlyList<SafeSaveResult> FileResults { get; init; }

    /// <summary>A short human-readable summary.</summary>
    public string? Message { get; init; }
}

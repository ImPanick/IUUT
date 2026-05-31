namespace IUUT.Core.Validation;

/// <summary>
/// The outcome of a validation pass (CODE_STYLE §4: structured result, not exceptions).
/// Errors block a write; warnings are shown for the user to confirm (master doc §13).
/// </summary>
public sealed record ValidationResult
{
    /// <summary>All findings (errors and warnings).</summary>
    public IReadOnlyList<ValidationIssue> Issues { get; init; } = [];

    /// <summary>Whether any finding is an <see cref="ValidationSeverity.Error"/>.</summary>
    public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error);

    /// <summary>True when there are no errors (warnings do not block).</summary>
    public bool IsValid => !HasErrors;

    /// <summary>The error findings.</summary>
    public IEnumerable<ValidationIssue> Errors => Issues.Where(i => i.Severity == ValidationSeverity.Error);

    /// <summary>The warning findings.</summary>
    public IEnumerable<ValidationIssue> Warnings => Issues.Where(i => i.Severity == ValidationSeverity.Warning);

    /// <summary>A result with no issues.</summary>
    public static ValidationResult Ok { get; } = new();

    /// <summary>Builds a result from issues.</summary>
    public static ValidationResult FromIssues(IEnumerable<ValidationIssue> issues) =>
        new() { Issues = issues.ToList() };

    /// <summary>Merges several results into one.</summary>
    public static ValidationResult Combine(params ValidationResult[] results)
    {
        ArgumentNullException.ThrowIfNull(results);
        return new ValidationResult { Issues = results.SelectMany(r => r.Issues).ToList() };
    }
}

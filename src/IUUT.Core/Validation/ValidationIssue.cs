namespace IUUT.Core.Validation;

/// <summary>A single validation finding.</summary>
/// <param name="Severity">Error (blocks) or Warning (confirm-to-proceed).</param>
/// <param name="Code">Stable machine-readable code, e.g. <c>profile-userid-mismatch</c>.</param>
/// <param name="Message">Human-readable explanation.</param>
/// <param name="Target">Optional pointer to the offending file/field.</param>
public sealed record ValidationIssue(
    ValidationSeverity Severity,
    string Code,
    string Message,
    string? Target = null);

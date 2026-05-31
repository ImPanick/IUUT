namespace IUUT.Core.Validation;

/// <summary>How serious a validation issue is (master doc §13).</summary>
public enum ValidationSeverity
{
    /// <summary>Soft warning — show it, let the user confirm and proceed (§13.2).</summary>
    Warning,

    /// <summary>Hard failure — block the write (§13.1).</summary>
    Error,
}

namespace IUUT.App.Services;

/// <summary>
/// Implemented by Custom editors that stage edits in memory until Apply. The Custom shell checks
/// this before swapping the editor away (category or save-profile switch), so staged work is never
/// silently discarded (Tier 1 dirty guard). Editors reset the flag on load and after apply.
/// </summary>
public interface IDirtyEditor
{
    /// <summary>True when the editor holds edits that have not been applied.</summary>
    bool IsDirty { get; }
}

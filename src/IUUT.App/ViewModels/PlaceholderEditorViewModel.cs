using Wpf.Ui.Controls;

namespace IUUT.App.ViewModels;

/// <summary>
/// The editor panel shown for a category that isn't wired to an interactive editor yet (its Core
/// service is built — see <see cref="CustomCategory.Status"/> — only the UI is pending), or before
/// a save profile is chosen. Pure display; mirrors the category's glyph/label/description/status.
/// </summary>
public sealed class PlaceholderEditorViewModel
{
    /// <summary>Creates the placeholder for a category.</summary>
    public PlaceholderEditorViewModel(CustomCategory category, bool needsProfile)
    {
        ArgumentNullException.ThrowIfNull(category);
        Glyph = category.Glyph;
        Label = category.Label;
        Description = category.Description;
        Status = category.Status;
        Hint = needsProfile
            ? "Select a save profile above to begin."
            : "This editor is coming soon — its Core service is ready and tested.";
    }

    /// <summary>Category line icon.</summary>
    public SymbolRegular Glyph { get; }

    /// <summary>Category label.</summary>
    public string Label { get; }

    /// <summary>What the category edits.</summary>
    public string Description { get; }

    /// <summary>Which Core service backs it / build status.</summary>
    public string Status { get; }

    /// <summary>A next-step hint for the user.</summary>
    public string Hint { get; }
}

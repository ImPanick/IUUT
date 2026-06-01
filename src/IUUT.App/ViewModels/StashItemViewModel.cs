namespace IUUT.App.ViewModels;

/// <summary>
/// One orbital-stash item in the Stash editor (master §8.6): its catalog <see cref="Label"/>, the
/// raw <see cref="RowName"/>, its <see cref="DatabaseGuid"/>, the stack count, and whether a loadout
/// still references it (a warning before removal). Read-only display; add/remove is the editor's job.
/// </summary>
public sealed class StashItemViewModel
{
    /// <summary>Creates a stash-item row.</summary>
    public StashItemViewModel(string rowName, string label, string databaseGuid, int stack, bool isReferenced)
    {
        RowName = string.IsNullOrEmpty(rowName) ? "(unknown)" : rowName;
        Label = string.IsNullOrEmpty(label) ? RowName : label;
        DatabaseGuid = databaseGuid ?? "";
        Stack = stack;
        IsReferenced = isReferenced;
    }

    /// <summary>The item's <c>D_ItemsStatic</c> row key.</summary>
    public string RowName { get; }

    /// <summary>The display name (catalog label, falling back to the key).</summary>
    public string Label { get; }

    /// <summary>The item's unique id.</summary>
    public string DatabaseGuid { get; }

    /// <summary>The item's stack count.</summary>
    public int Stack { get; }

    /// <summary>The stack count as a display chip (e.g. <c>×42</c>).</summary>
    public string StackLabel => $"×{Stack}";

    /// <summary>Whether a loadout references this item (warn before removing it).</summary>
    public bool IsReferenced { get; }

    /// <summary>A short reference hint for the list.</summary>
    public string ReferenceHint => IsReferenced ? "⚠ referenced by a loadout" : "";
}

namespace IUUT.App.ViewModels;

/// <summary>
/// One orbital-stash item in the Stash editor (master §8.6): catalog <see cref="Label"/>, raw
/// <see cref="RowName"/>, <see cref="DatabaseGuid"/>, stack count, durability state, and whether a
/// loadout references it. Read-only display; add/remove/repair is the editor's job.
/// </summary>
public sealed class StashItemViewModel
{
    /// <summary>Creates a stash-item row.</summary>
    public StashItemViewModel(
        string rowName,
        string label,
        string databaseGuid,
        int stack,
        int? durability,
        int? maxDurability,
        bool isReferenced)
    {
        RowName = string.IsNullOrEmpty(rowName) ? "(unknown)" : rowName;
        Label = string.IsNullOrEmpty(label) ? RowName : label;
        DatabaseGuid = databaseGuid ?? "";
        Stack = stack;
        Durability = durability;
        MaxDurability = maxDurability;
        IsReferenced = isReferenced;
    }

    /// <summary>The item's <c>D_ItemsStatic</c> row key.</summary>
    public string RowName { get; }

    /// <summary>The display name (catalog label, falling back to a humanized key).</summary>
    public string Label { get; }

    /// <summary>The item's unique id.</summary>
    public string DatabaseGuid { get; }

    /// <summary>The item's stack count.</summary>
    public int Stack { get; }

    /// <summary>The stack count as a display chip (e.g. <c>×42</c>).</summary>
    public string StackLabel => $"×{Stack}";

    /// <summary>Current durability, or <c>null</c> when the item isn't durable.</summary>
    public int? Durability { get; }

    /// <summary>The max durability observed for this item type in the stash (the repair target).</summary>
    public int? MaxDurability { get; }

    /// <summary>Whether this is a durable item (shows a durability chip).</summary>
    public bool HasDurability => Durability is not null;

    /// <summary>Whether the item is below its (observed) max durability.</summary>
    public bool IsDamaged => Durability is not null && MaxDurability is not null && Durability < MaxDurability;

    /// <summary>Durability chip text (e.g. <c>🔧 3486 / 5500</c>), empty when not durable.</summary>
    public string DurabilityLabel
    {
        get
        {
            if (Durability is null)
            {
                return "";
            }

            return MaxDurability is > 0 && MaxDurability != Durability
                ? $"🔧 {Durability:N0} / {MaxDurability:N0}"
                : $"🔧 {Durability:N0}";
        }
    }

    /// <summary>Whether a loadout references this item (warn before removing it).</summary>
    public bool IsReferenced { get; }

    /// <summary>A short reference hint for the list.</summary>
    public string ReferenceHint => IsReferenced ? "⚠ referenced by a loadout" : "";
}

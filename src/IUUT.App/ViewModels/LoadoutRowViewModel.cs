using IUUT.Core.Catalog;
using IUUT.Core.Editing;

namespace IUUT.App.ViewModels;

/// <summary>
/// One per-prospect loadout in the read-only Loadouts viewer (master §8.7) — shown by the prospect
/// it is for and the gear it carries, with item RowNames resolved to friendly catalog names.
/// </summary>
public sealed class LoadoutRowViewModel
{
    /// <summary>Builds the row from a Core <see cref="LoadoutSummary"/>, resolving item names via the catalog.</summary>
    public LoadoutRowViewModel(LoadoutSummary summary, GameCatalogs catalogs)
    {
        ArgumentNullException.ThrowIfNull(summary);
        ArgumentNullException.ThrowIfNull(catalogs);

        ChrSlot = summary.ChrSlot;
        ProspectId = string.IsNullOrEmpty(summary.ProspectId) ? "—" : summary.ProspectId;
        EnviroSuit = summary.EnviroSuitRowName is null ? "— none —" : catalogs.Items.Label(summary.EnviroSuitRowName);

        Items = summary.Items
            .Select(i => i.Count > 1 ? $"{catalogs.Items.Label(i.RowName)}   ×{i.Count}" : catalogs.Items.Label(i.RowName))
            .ToList();
        ItemCount = summary.Items.Sum(i => i.Count);

        var flags = new List<string>();
        if (!string.IsNullOrEmpty(summary.ProspectState))
        {
            flags.Add(summary.ProspectState);
        }

        if (summary.Insured)
        {
            flags.Add("insured");
        }

        if (summary.Settled)
        {
            flags.Add("settled");
        }

        Flags = string.Join("   ·   ", flags);
    }

    /// <summary>The character slot this loadout is for.</summary>
    public int ChrSlot { get; }

    /// <summary>The prospect this loadout is configured for.</summary>
    public string ProspectId { get; }

    /// <summary>The envirosuit's friendly name, or an em-dash placeholder when empty.</summary>
    public string EnviroSuit { get; }

    /// <summary>The meta items the loadout carries, by friendly name (with a ×count when stacked).</summary>
    public IReadOnlyList<string> Items { get; }

    /// <summary>Total meta items carried.</summary>
    public int ItemCount { get; }

    /// <summary>State + insured/settled, joined for a muted sub-line.</summary>
    public string Flags { get; }

    /// <summary>Card heading: prospect first, then the slot.</summary>
    public string Title => $"{ProspectId}   ·   Slot {ChrSlot}";

    /// <summary>Items panel header.</summary>
    public string ItemsHeader => ItemCount == 0 ? "No meta items" : $"Meta items ({ItemCount})";

    /// <summary>Whether the loadout carries any meta items (drives the empty state).</summary>
    public bool HasItems => Items.Count > 0;
}

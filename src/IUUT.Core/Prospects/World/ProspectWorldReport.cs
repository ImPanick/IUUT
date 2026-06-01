namespace IUUT.Core.Prospects.World;

/// <summary>One item found in a prospect world blob: its <c>D_ItemsStatic</c> RowName and friendly name.</summary>
public sealed record ProspectWorldItem(string RowName, string DisplayName);

/// <summary>
/// Read-only summary of what a prospect world blob contains (the spike's "what can we see?" result):
/// how many actor records, inventories, and slots, and every item instance found, with friendly names
/// resolved via the items catalog. This is an inspection report — no edit capability is implied.
/// </summary>
public sealed class ProspectWorldReport
{
    /// <summary>Number of actor state records (the <c>StateRecorderBlobs</c> array elements).</summary>
    public int ActorRecordCount { get; init; }

    /// <summary>Number of <c>InventorySaveData</c> structs (player + container + deployable inventories).</summary>
    public int InventoryCount { get; init; }

    /// <summary>Number of <c>InventorySlotSaveData</c> structs (individual inventory slots).</summary>
    public int SlotCount { get; init; }

    /// <summary>Every item instance found (one entry per filled slot), in document order.</summary>
    public IReadOnlyList<ProspectWorldItem> Items { get; init; } = Array.Empty<ProspectWorldItem>();

    /// <summary>Distinct item RowName → instance count, for a compact roll-up.</summary>
    public IReadOnlyDictionary<string, int> ItemCounts { get; init; } =
        new Dictionary<string, int>(StringComparer.Ordinal);
}

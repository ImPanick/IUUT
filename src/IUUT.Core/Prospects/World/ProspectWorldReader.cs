using IUUT.Core.Catalog;
using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>
/// Read-only inspector for a prospect world blob (the in-prospect inventory/container spike). Parses the
/// decompressed <c>StateRecorderBlobs</c> UE property tree and surfaces a <see cref="ProspectWorldReport"/>:
/// actor/inventory/slot counts and every item instance (the <c>ItemStaticData</c> NameProperty per filled
/// slot), with friendly names resolved via the items catalog. See docs/DATA-PROVENANCE.md
/// "Prospect world-save anatomy". This proves the read path; no mutation is performed.
/// </summary>
public sealed class ProspectWorldReader
{
    /// <summary>The struct name of a single inventory record in the world blob.</summary>
    public const string InventoryStruct = "InventorySaveData";

    /// <summary>The struct name of a single inventory slot in the world blob.</summary>
    public const string SlotStruct = "InventorySlotSaveData";

    /// <summary>The NameProperty whose value is the item's <c>D_ItemsStatic</c> RowName.</summary>
    public const string ItemStaticDataProperty = "ItemStaticData";

    /// <summary>The top-level array of per-actor state records.</summary>
    public const string RecorderArray = "StateRecorderBlobs";

    private readonly CatalogTable _items;

    /// <summary>Creates the reader with the items catalog used to resolve friendly names.</summary>
    public ProspectWorldReader(CatalogTable items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _items = items;
    }

    /// <summary>Decompresses and inspects a <see cref="ProspectBlobModel"/> from a prospect save.</summary>
    public ProspectWorldReport ReadBlob(ProspectBlobModel blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        return Read(ProspectBlobVerifier.Decompress(blob.BinaryBlob));
    }

    /// <summary>Inspects the already-decompressed UE property bytes of a prospect world blob.</summary>
    public ProspectWorldReport Read(byte[] decompressed)
    {
        ArgumentNullException.ThrowIfNull(decompressed);
        var tree = UePropertyReader.ReadStream(decompressed);

        var items = new List<ProspectWorldItem>();
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var inventoryCount = 0;
        var slotCount = 0;

        foreach (var node in tree)
        {
            Visit(node, decompressed, items, counts, ref inventoryCount, ref slotCount);
        }

        var actorRecords = tree
            .FirstOrDefault(p => string.Equals(p.Name, RecorderArray, StringComparison.Ordinal))?
            .Children.Count ?? 0;

        return new ProspectWorldReport
        {
            ActorRecordCount = actorRecords,
            InventoryCount = inventoryCount,
            SlotCount = slotCount,
            Items = items,
            ItemCounts = counts,
        };
    }

    private void Visit(
        UeProperty node,
        byte[] data,
        List<ProspectWorldItem> items,
        Dictionary<string, int> counts,
        ref int inventoryCount,
        ref int slotCount)
    {
        if (string.Equals(node.StructName, InventoryStruct, StringComparison.Ordinal))
        {
            inventoryCount++;
        }
        else if (string.Equals(node.StructName, SlotStruct, StringComparison.Ordinal))
        {
            slotCount++;
        }

        if (string.Equals(node.Name, ItemStaticDataProperty, StringComparison.Ordinal) &&
            string.Equals(node.Type, "NameProperty", StringComparison.Ordinal))
        {
            var pos = node.ValueOffset;
            var rowName = UePropertyReader.ReadFString(data, ref pos);
            if (!string.IsNullOrEmpty(rowName))
            {
                items.Add(new ProspectWorldItem(rowName, _items.Label(rowName)));
                counts[rowName] = counts.TryGetValue(rowName, out var c) ? c + 1 : 1;
            }
        }

        foreach (var child in node.Children)
        {
            Visit(child, data, items, counts, ref inventoryCount, ref slotCount);
        }
    }
}

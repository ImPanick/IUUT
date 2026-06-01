using System.Text.Json;
using IUUT.Core.Abstractions;
using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// Custom-mode edits to the orbital stash (<c>MetaInventory.json</c>, master doc §8.6, §10.4).
/// Adds items with a freshly-minted <c>DatabaseGUID</c> and removes by GUID. Pure in-memory
/// mutation; uniqueness is enforced by <c>ValidationEngine.ValidateUniqueDatabaseGuids</c> and the
/// write goes through <c>CustomApplyService</c>. The visual stash grid (WP-24) is parked with the UI.
/// </summary>
public sealed class StashEditService
{
    /// <summary>The data-table all stash items reference.</summary>
    public const string ItemsDataTable = "D_ItemsStatic";

    /// <summary>The <c>ItemDynamicData</c> property that holds an item's stack count.</summary>
    public const string StackProperty = "ItemableStack";

    /// <summary>The <c>ItemDynamicData</c> property that holds an item's current durability.</summary>
    public const string DurabilityProperty = "Durability";

    /// <summary>The hard game-set maximum stack size; the stash editor clamps to this (owner, 2026-05-31).</summary>
    public const int MaxStack = 100;

    private readonly IGuidProvider _guids;

    /// <summary>Creates the stash editor over the GUID source (so item ids are deterministic in tests).</summary>
    public StashEditService(IGuidProvider guids)
    {
        ArgumentNullException.ThrowIfNull(guids);
        _guids = guids;
    }

    /// <summary>A fresh stash item id: 32 hex digits, uppercase, no dashes (matches the game's format).</summary>
    public string NewDatabaseGuid() => _guids.NewGuid().ToString("N").ToUpperInvariant();

    /// <summary>
    /// Adds a new stash item for <paramref name="rowName"/> with a fresh GUID and
    /// <c>ItemOwnerLookupId = -1</c>, and returns it. Dynamic data (durability/stack) is populated
    /// by the stash UI from the catalog (WP-24).
    /// </summary>
    public MetaItem AddItem(MetaInventoryModel inventory, string rowName)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentException.ThrowIfNullOrEmpty(rowName);

        var item = new MetaItem
        {
            ItemStaticData = new DataTableRef { RowName = rowName, DataTableName = ItemsDataTable },
            DatabaseGuid = NewDatabaseGuid(),
            ItemOwnerLookupId = -1,
        };
        inventory.Items.Add(item);
        return item;
    }

    /// <summary>
    /// Sets an item's stack count, clamped to 1..<see cref="MaxStack"/> (the hard game limit), adding
    /// or updating the <c>ItemableStack</c> dynamic property. Other dynamic data is preserved.
    /// </summary>
    public void SetStack(MetaItem item, int count)
    {
        ArgumentNullException.ThrowIfNull(item);
        SetIntProperty(item, StackProperty, Math.Clamp(count, 1, MaxStack));
    }

    /// <summary>The item's current stack count from its <c>ItemableStack</c> property, or 1 if absent/non-numeric.</summary>
    public static int GetStack(MetaItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetIntProperty(item, StackProperty) ?? 1;
    }

    /// <summary>Sets an item's current durability (clamped to ≥ 0), adding/updating the <c>Durability</c> property.</summary>
    public void SetDurability(MetaItem item, int durability)
    {
        ArgumentNullException.ThrowIfNull(item);
        SetIntProperty(item, DurabilityProperty, Math.Max(0, durability));
    }

    /// <summary>The item's current durability, or <c>null</c> when it has no <c>Durability</c> property (not a durable item).</summary>
    public static int? GetDurability(MetaItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return GetIntProperty(item, DurabilityProperty);
    }

    private static void SetIntProperty(MetaItem item, string propertyType, int value)
    {
        var element = JsonSerializer.SerializeToElement(value);
        var existing = item.ItemDynamicData.FirstOrDefault(p => string.Equals(p.PropertyType, propertyType, StringComparison.Ordinal));
        if (existing is not null)
        {
            existing.Value = element;
        }
        else
        {
            item.ItemDynamicData.Add(new ItemDynamicProperty { PropertyType = propertyType, Value = element });
        }
    }

    private static int? GetIntProperty(MetaItem item, string propertyType)
    {
        var property = item.ItemDynamicData.FirstOrDefault(p => string.Equals(p.PropertyType, propertyType, StringComparison.Ordinal));
        return property is not null && property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out var value)
            ? value
            : null;
    }

    /// <summary>Removes the stash item with <paramref name="databaseGuid"/>; returns whether it was present.</summary>
    public bool RemoveItem(MetaInventoryModel inventory, string databaseGuid)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentException.ThrowIfNullOrEmpty(databaseGuid);

        var item = inventory.Items.FirstOrDefault(i => string.Equals(i.DatabaseGuid, databaseGuid, StringComparison.OrdinalIgnoreCase));
        return item is not null && inventory.Items.Remove(item);
    }
}

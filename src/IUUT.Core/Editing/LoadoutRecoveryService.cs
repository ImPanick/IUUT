using System.Text.Json;
using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// Loadout recovery (roadmap Tier 2): the two community hand-edits, made safe. (1) INSURE —
/// gear stuck with an offline host is recovered by flipping the loadout's top-level
/// <c>bInsured</c> flag; players fear corrupting the file by hand. (2) RESTORE — loadouts
/// reference stash items by <c>DatabaseGUID</c>; when the item vanished from
/// <c>MetaInventory.json</c> the reference dangles and the loadout is broken. Restoring
/// recreates the item in the stash with the EXACT referenced GUID and its RowName (read from
/// the loadout's own item block), making the loadout whole again. Both are additive: no
/// loadout sub-block is ever removed or rewritten beyond the one boolean (CONSTITUTION VI).
/// </summary>
public sealed class LoadoutRecoveryService
{
    /// <summary>One dangling reference: the missing stash GUID and, when the loadout block names it, the item row.</summary>
    public sealed record DanglingItem(string DatabaseGuid, string? RowName);

    /// <summary>What recovery would do: how many loadouts are uninsured and which references dangle.</summary>
    public sealed record RecoveryPreview(int UninsuredLoadouts, IReadOnlyList<DanglingItem> Dangling)
    {
        /// <summary>Dangling references that carry a RowName and can therefore be restored.</summary>
        public int Restorable => Dangling.Count(d => d.RowName is not null);
    }

    /// <summary>Previews recovery for <paramref name="loadouts"/> against <paramref name="stash"/> (read-only).</summary>
    public RecoveryPreview Preview(LoadoutsModel loadouts, MetaInventoryModel stash)
    {
        ArgumentNullException.ThrowIfNull(loadouts);
        ArgumentNullException.ThrowIfNull(stash);

        var uninsured = loadouts.Loadouts.Count(entry => !IsInsured(entry));

        var present = new HashSet<string>(stash.Items.Select(i => i.DatabaseGuid), StringComparer.OrdinalIgnoreCase);
        var dangling = CollectItemReferences(loadouts)
            .Where(r => !present.Contains(r.DatabaseGuid))
            .ToList();

        return new RecoveryPreview(uninsured, dangling);
    }

    /// <summary>Sets <c>bInsured</c> true on every loadout where it is not already; returns how many changed.</summary>
    public int InsureAll(LoadoutsModel loadouts)
    {
        ArgumentNullException.ThrowIfNull(loadouts);

        var changed = 0;
        foreach (var entry in loadouts.Loadouts)
        {
            // An entry with no sub-blocks at all is degenerate — inventing a dictionary for it
            // would guess at schema, so it is skipped (never seen in real saves).
            if (entry.AdditionalData is null || IsInsured(entry))
            {
                continue;
            }

            entry.AdditionalData["bInsured"] = JsonSerializer.SerializeToElement(true);
            changed++;
        }

        return changed;
    }

    /// <summary>
    /// Recreates every restorable dangling reference as a stash item with the EXACT referenced
    /// GUID and RowName (<c>ItemOwnerLookupId = -1</c> like all stash items). Returns how many
    /// items were added. References without a RowName cannot be recreated and are left alone.
    /// </summary>
    public int RestoreDangling(LoadoutsModel loadouts, MetaInventoryModel stash)
    {
        ArgumentNullException.ThrowIfNull(loadouts);
        ArgumentNullException.ThrowIfNull(stash);

        var added = 0;
        foreach (var item in Preview(loadouts, stash).Dangling)
        {
            if (item.RowName is null)
            {
                continue;
            }

            stash.Items.Add(new MetaItem
            {
                ItemStaticData = new DataTableRef { RowName = item.RowName, DataTableName = StashEditService.ItemsDataTable },
                DatabaseGuid = item.DatabaseGuid,
                ItemOwnerLookupId = -1,
            });
            added++;
        }

        return added;
    }

    private static bool IsInsured(LoadoutEntry entry) =>
        entry.AdditionalData is { } data &&
        data.TryGetValue("bInsured", out var value) &&
        value.ValueKind == JsonValueKind.True;

    // Every (DatabaseGUID, RowName) pair inside the loadouts' item blocks. The RowName comes from
    // the SAME object's ItemStaticData, so a restored item recreates what the loadout expects.
    private static List<DanglingItem> CollectItemReferences(LoadoutsModel loadouts)
    {
        var references = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in loadouts.Loadouts)
        {
            if (entry.AdditionalData is null)
            {
                continue;
            }

            foreach (var element in entry.AdditionalData.Values)
            {
                Collect(element, references);
            }
        }

        return references.Select(kv => new DanglingItem(kv.Key, kv.Value)).ToList();
    }

    private static void Collect(JsonElement element, Dictionary<string, string?> references)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("DatabaseGUID", out var guid) &&
                    guid.ValueKind == JsonValueKind.String &&
                    guid.GetString() is { Length: > 0 } guidValue)
                {
                    var rowName = RowNameOf(element);
                    // Keep a RowName if any block for this GUID names one.
                    if (!references.TryGetValue(guidValue, out var known) || known is null)
                    {
                        references[guidValue] = rowName;
                    }
                }

                foreach (var property in element.EnumerateObject())
                {
                    Collect(property.Value, references);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Collect(item, references);
                }

                break;
            default:
                break;
        }
    }

    private static string? RowNameOf(JsonElement itemBlock)
    {
        if (itemBlock.TryGetProperty("ItemStaticData", out var staticData) &&
            staticData.ValueKind == JsonValueKind.Object &&
            staticData.TryGetProperty("RowName", out var rowName) &&
            rowName.ValueKind == JsonValueKind.String &&
            rowName.GetString() is { Length: > 0 } value &&
            !string.Equals(value, "None", StringComparison.Ordinal))
        {
            return value;
        }

        return null;
    }
}

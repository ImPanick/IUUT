using System.Text.Json;
using IUUT.Core.Models;

namespace IUUT.Core.Editing;

/// <summary>
/// The GUID coupling between loadouts and the stash (master doc §8.7, §10.4). Loadout sub-blocks
/// (EnviroSuit, MetaItems, …) embed item <c>DatabaseGUID</c>s that may point at
/// <c>MetaInventory.json</c> items; this finds those references so the stash editor can warn before
/// removing a referenced item and surface dangling references. Read-only.
/// </summary>
public sealed class LoadoutCrossReference
{
    private const string GuidProperty = "DatabaseGUID";

    /// <summary>All item <c>DatabaseGUID</c>s referenced anywhere inside the loadouts.</summary>
    public ISet<string> ReferencedDatabaseGuids(LoadoutsModel loadouts)
    {
        ArgumentNullException.ThrowIfNull(loadouts);

        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in loadouts.Loadouts)
        {
            if (entry.AdditionalData is null)
            {
                continue;
            }

            foreach (var element in entry.AdditionalData.Values)
            {
                Collect(element, found);
            }
        }

        return found;
    }

    /// <summary>
    /// Digests each loadout into a human-readable <see cref="LoadoutSummary"/> — the prospect it is
    /// for, its envirosuit, and its meta items (grouped by RowName) — so the viewer can show names
    /// instead of raw GUIDs. Read-only; tolerant of missing sub-blocks.
    /// </summary>
    public IReadOnlyList<LoadoutSummary> Summarize(LoadoutsModel loadouts)
    {
        ArgumentNullException.ThrowIfNull(loadouts);

        var summaries = new List<LoadoutSummary>(loadouts.Loadouts.Count);
        foreach (var entry in loadouts.Loadouts)
        {
            var data = entry.AdditionalData;
            summaries.Add(new LoadoutSummary(
                entry.ChrSlot,
                SubField(data, "AssociatedProspect", "ProspectID") ?? "",
                SubField(data, "AssociatedProspect", "ProspectState") ?? "",
                EnviroSuitRowName(data),
                MetaItemRefs(data),
                Flag(data, "bInsured"),
                Flag(data, "bSettled"),
                entry.LoadoutGuid));
        }

        return summaries;
    }

    /// <summary>Whether any loadout references the stash item <paramref name="databaseGuid"/> (warn before removing it).</summary>
    public bool IsReferenced(LoadoutsModel loadouts, string databaseGuid)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseGuid);
        return ReferencedDatabaseGuids(loadouts).Contains(databaseGuid);
    }

    /// <summary>Loadout-referenced GUIDs that are NOT present in the stash (dangling references to restore together).</summary>
    public IReadOnlyList<string> DanglingReferences(LoadoutsModel loadouts, MetaInventoryModel stash)
    {
        ArgumentNullException.ThrowIfNull(stash);

        var present = new HashSet<string>(stash.Items.Select(i => i.DatabaseGuid), StringComparer.OrdinalIgnoreCase);
        return ReferencedDatabaseGuids(loadouts).Where(g => !present.Contains(g)).ToList();
    }

    private static void Collect(JsonElement element, ISet<string> found)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals(GuidProperty) && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            found.Add(value);
                        }
                    }

                    Collect(property.Value, found);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Collect(item, found);
                }

                break;
            default:
                break;
        }
    }

    // An item sub-block ("EnviroSuit", a "MetaItems" element) → its ItemStaticData RowName, or null
    // for the empty "None" slot / a missing block.
    private static string? RowName(JsonElement itemBlock)
    {
        if (itemBlock.ValueKind == JsonValueKind.Object &&
            itemBlock.TryGetProperty("ItemStaticData", out var staticData) &&
            staticData.ValueKind == JsonValueKind.Object &&
            staticData.TryGetProperty("RowName", out var rowName) &&
            rowName.ValueKind == JsonValueKind.String)
        {
            var value = rowName.GetString();
            return string.IsNullOrEmpty(value) || string.Equals(value, "None", StringComparison.Ordinal) ? null : value;
        }

        return null;
    }

    private static string? EnviroSuitRowName(Dictionary<string, JsonElement>? data) =>
        data is not null && data.TryGetValue("EnviroSuit", out var enviroSuit) ? RowName(enviroSuit) : null;

    private static List<LoadoutItemRef> MetaItemRefs(Dictionary<string, JsonElement>? data)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        if (data is not null && data.TryGetValue("MetaItems", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in items.EnumerateArray())
            {
                if (RowName(item) is { } rowName)
                {
                    counts[rowName] = counts.GetValueOrDefault(rowName) + 1;
                }
            }
        }

        return counts
            .Select(kv => new LoadoutItemRef(kv.Key, kv.Value))
            .OrderBy(r => r.RowName, StringComparer.Ordinal)
            .ToList();
    }

    private static string? SubField(Dictionary<string, JsonElement>? data, string block, string field) =>
        data is not null && data.TryGetValue(block, out var sub) && sub.ValueKind == JsonValueKind.Object &&
        sub.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool Flag(Dictionary<string, JsonElement>? data, string field) =>
        data is not null && data.TryGetValue(field, out var value) && value.ValueKind == JsonValueKind.True;
}

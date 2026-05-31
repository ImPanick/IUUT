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
}

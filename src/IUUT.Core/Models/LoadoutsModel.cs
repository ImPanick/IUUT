using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// <c>Loadout\Loadouts.json</c> — per-prospect drop loadouts (master doc §8.7). Item GUIDs in
/// loadouts may reference <c>MetaInventory.json</c>; restore the two together and warn before
/// removing a stash item a loadout still references (see <c>LoadoutCrossReference</c>). Unknown
/// top-level members round-trip verbatim (CONSTITUTION VI).
/// </summary>
public sealed class LoadoutsModel
{
    /// <summary>The loadout entries.</summary>
    public List<LoadoutEntry> Loadouts { get; set; } = [];

    /// <summary>Unknown top-level members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

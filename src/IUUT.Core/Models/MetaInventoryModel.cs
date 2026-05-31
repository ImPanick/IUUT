using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// <c>MetaInventory.json</c> — the orbital workshop stash (master doc §8.6). The game does
/// <b>not</b> rotate backups for this file, so IUUT's own pre-write backup is its only safety net
/// (field guide §7.6). Unknown top-level members round-trip verbatim (CONSTITUTION VI).
/// </summary>
public sealed class MetaInventoryModel
{
    /// <summary>The inventory id, e.g. <c>MetaInventoryID_Main</c>.</summary>
    [JsonPropertyName("InventoryID")]
    public string InventoryId { get; set; } = "";

    /// <summary>The stash items.</summary>
    public List<MetaItem> Items { get; set; } = [];

    /// <summary>Unknown top-level members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

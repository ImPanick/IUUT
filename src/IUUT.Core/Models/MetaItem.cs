using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// One item in the orbital stash (<see cref="MetaInventoryModel.Items"/>, master doc §8.6). IUUT
/// edits <see cref="ItemStaticData"/> (Replace), <see cref="ItemDynamicData"/> (Durability /
/// ItemableStack), and mints a fresh <see cref="DatabaseGuid"/> on add. The richer per-item
/// blocks (ItemCustomStats, CustomProperties, RuntimeTags) round-trip verbatim via
/// <see cref="AdditionalData"/> (CONSTITUTION VI).
/// </summary>
public sealed class MetaItem
{
    /// <summary>The item's <c>D_ItemsStatic</c> reference (its identity / display row).</summary>
    public DataTableRef ItemStaticData { get; set; } = new();

    /// <summary>Per-instance dynamic properties (stack count, durability, …).</summary>
    public List<ItemDynamicProperty> ItemDynamicData { get; set; } = [];

    /// <summary>Globally-unique item id (32 hex, uppercase, no dashes). Fresh per added item; must be unique (§13.1).</summary>
    [JsonPropertyName("DatabaseGUID")]
    public string DatabaseGuid { get; set; } = "";

    /// <summary>Owner lookup; always <c>-1</c> for stash items (master §8.6).</summary>
    public int ItemOwnerLookupId { get; set; } = -1;

    /// <summary>Unknown members (ItemCustomStats, CustomProperties, RuntimeTags, …) preserved verbatim (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// One entry in a stash item's <c>ItemDynamicData</c> (master doc §8.6), e.g.
/// <c>{ "PropertyType": "Durability", "Value": 5500 }</c>. <see cref="Value"/> is kept as a
/// raw <see cref="JsonElement"/> so any numeric/other shape round-trips verbatim (CONSTITUTION VI);
/// the stash editor reads/writes the well-known integer properties (Durability, ItemableStack).
/// </summary>
public sealed class ItemDynamicProperty
{
    /// <summary>The property name, e.g. <c>Durability</c> or <c>ItemableStack</c>.</summary>
    public string PropertyType { get; set; } = "";

    /// <summary>The value, preserved verbatim.</summary>
    public JsonElement Value { get; set; }

    /// <summary>Unknown members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

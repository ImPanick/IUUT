using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// One per-prospect loadout (master doc §8.7): links a character slot to an envirosuit + meta
/// items for a drop. Only the cross-cutting fields are modelled; the rich sub-blocks (EnviroSuit,
/// Dropship, MetaItems, AssociatedProspect, HostedBy, insurance flags, …) round-trip verbatim via
/// <see cref="AdditionalData"/> (CONSTITUTION VI). Item <c>DatabaseGUID</c>s live inside those
/// sub-blocks and are read by <c>LoadoutCrossReference</c>.
/// </summary>
public sealed class LoadoutEntry
{
    /// <summary>The character slot this loadout belongs to; must match a <c>Characters.json</c> slot.</summary>
    public int ChrSlot { get; set; }

    /// <summary>The loadout-level id (independent of the item <c>DatabaseGUID</c>s it references).</summary>
    [JsonPropertyName("Guid")]
    public string LoadoutGuid { get; set; } = "";

    /// <summary>Unknown members (EnviroSuit, MetaItems, Dropship, HostedBy, …) preserved verbatim (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

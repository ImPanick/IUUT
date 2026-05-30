using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// <c>BestiaryData.json</c> — scan progress (master doc §8.5). Two top-level keys on
/// the live save: <see cref="BestiaryTracking"/> (creature groups; the Bestiary editor
/// target) and <see cref="FishTracking"/> (fishing/aquatic records). Unknown members
/// round-trip verbatim via <see cref="AdditionalData"/> (CONSTITUTION VI).
/// </summary>
public sealed class BestiaryModel
{
    /// <summary>Creature-group scan progress; Lazy Max maxes <c>NumPoints</c> here.</summary>
    public List<BestiaryEntry> BestiaryTracking { get; set; } = [];

    /// <summary>Fishing/aquatic scan records. Preserved as-is unless a fishing feature targets it.</summary>
    public List<FishEntry> FishTracking { get; set; } = [];

    /// <summary>Unknown top-level members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

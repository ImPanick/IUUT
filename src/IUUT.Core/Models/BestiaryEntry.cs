using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>One creature-group scan record in <see cref="BestiaryModel.BestiaryTracking"/> (master §8.5).</summary>
public sealed class BestiaryEntry
{
    /// <summary>The creature group's <c>D_BestiaryData</c> reference.</summary>
    public DataTableRef BestiaryGroup { get; set; } = new();

    /// <summary>Accumulated scan points for the group.</summary>
    public long NumPoints { get; set; }

    /// <summary>Unknown members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

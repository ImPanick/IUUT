using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// A <c>{ RowName, Rank }</c> talent entry. The same JSON shape is used by both
/// <see cref="ProfileModel.Talents"/> (workshop/blueprint unlocks, Rank always 1)
/// and character skill trees (Rank 0–4). Semantics differ; the shape is shared.
/// </summary>
public sealed class Talent
{
    /// <summary>The <c>D_Talents</c> row key, e.g. <c>Workshop_Envirosuit</c> or <c>Genetics_Twins</c>.</summary>
    public string RowName { get; set; } = "";

    /// <summary>The rank. The game clamps over-ranked values to each row's true max on load.</summary>
    public int Rank { get; set; }

    /// <summary>Unknown members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

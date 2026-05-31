using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// One claimed-prospect association in <c>AssociatedProspects_Slot_N.json</c> (master doc §8.8) —
/// a character's entry in the Continue menu. <see cref="ProspectId"/> is the identity used to
/// "unstick" a phantom association; the rest (state, difficulty, insurance, members) round-trips
/// verbatim via <see cref="AdditionalData"/> (CONSTITUTION VI) until the Custom UI models it.
/// </summary>
public sealed class AssociatedProspect
{
    /// <summary>The prospect id (display; delete this association to free a stuck character).</summary>
    [JsonPropertyName("ProspectID")]
    public string ProspectId { get; set; } = "";

    /// <summary>Unknown members (ProspectState, Difficulty, Insurance, AssociatedMembers, …) preserved verbatim (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

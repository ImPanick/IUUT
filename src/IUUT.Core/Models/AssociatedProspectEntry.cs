using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// One element of an <c>AssociatedProspects_Slot_N.json</c> array. The game wraps each association in an
/// <c>"AssociatedProspect"</c> object — the on-disk inner string is
/// <c>{ "AssociatedProspect": { "ProspectID": "Olympus", "ClaimedAccountID": …, "AssociatedMembers": [ … ] } }</c>.
/// The parser unwraps this so callers see the <see cref="AssociatedProspect"/> directly (its
/// <see cref="Models.AssociatedProspect.ProspectId"/> populated); the serializer re-wraps it.
/// </summary>
public sealed class AssociatedProspectEntry
{
    /// <summary>The wrapped association.</summary>
    [JsonPropertyName("AssociatedProspect")]
    public AssociatedProspect AssociatedProspect { get; set; } = new();

    /// <summary>Any sibling keys preserved verbatim (CONSTITUTION VI); none observed in practice.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

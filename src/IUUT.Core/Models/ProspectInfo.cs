using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// The editable header of a <c>Prospects\&lt;name&gt;.json</c> world save (master doc §8.9).
/// <see cref="ProspectId"/> and <see cref="ProspectDtKey"/> are the stable identifiers (never the
/// filename); other header fields round-trip verbatim via <see cref="AdditionalData"/>
/// (CONSTITUTION VI). Header edits are safe; the compressed world blob is never touched here.
/// </summary>
public sealed class ProspectInfo
{
    /// <summary>The prospect id (display identifier).</summary>
    [JsonPropertyName("ProspectID")]
    public string ProspectId { get; set; } = "";

    /// <summary>The data-table key, e.g. <c>Outpost006_Olympus</c> (the stable type identifier).</summary>
    [JsonPropertyName("ProspectDTKey")]
    public string ProspectDtKey { get; set; } = "";

    /// <summary>Unknown header members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

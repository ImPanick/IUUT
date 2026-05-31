using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// A <c>Prospects\&lt;name&gt;.json</c> world save (master doc §8.9): a safely-editable
/// <see cref="ProspectInfo"/> header plus the compressed <see cref="ProspectBlobModel"/> world
/// state. v1 edits the header only; the blob is preserved verbatim (its mutation is the
/// <c>ProspectBlobCodec</c>'s job, WP-28). Unknown top-level members round-trip (CONSTITUTION VI).
/// </summary>
public sealed class ProspectFileModel
{
    /// <summary>The editable header.</summary>
    public ProspectInfo ProspectInfo { get; set; } = new();

    /// <summary>The compressed world-state blob (preserved verbatim unless re-encoded via the codec).</summary>
    public ProspectBlobModel ProspectBlob { get; set; } = new();

    /// <summary>Unknown top-level members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

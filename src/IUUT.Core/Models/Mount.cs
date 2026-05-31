using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// One tamed mount in <see cref="MountsModel.SavedMounts"/> (master doc §8.10). IUUT edits the
/// denormalized JSON fields (name, level); the authoritative stats/talents live in
/// <c>RecorderBlob.BinaryData</c>, which round-trips verbatim via <see cref="AdditionalData"/>
/// (CONSTITUTION VI) — JSON-only edits are sufficient for UI fixes (binary editing is later).
/// </summary>
public sealed class Mount
{
    /// <summary>The mount's display name (editable).</summary>
    public string MountName { get; set; } = "";

    /// <summary>The mount's level — a denormalized UI copy (editable).</summary>
    public int MountLevel { get; set; }

    /// <summary>The mount type, e.g. <c>Arctic_Moa</c>.</summary>
    public string MountType { get; set; } = "";

    /// <summary>Links to <c>Mounts\&lt;id&gt;.exr</c>.</summary>
    public string MountIconName { get; set; } = "";

    /// <summary>Unknown members (RecorderBlob with the authoritative binary state, …) preserved verbatim (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

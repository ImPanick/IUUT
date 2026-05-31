using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// <c>Mounts.json</c> — the tamed-mount roster (master doc §8.10). Unknown top-level members
/// round-trip verbatim (CONSTITUTION VI).
/// </summary>
public sealed class MountsModel
{
    /// <summary>The saved mounts.</summary>
    public List<Mount> SavedMounts { get; set; } = [];

    /// <summary>Unknown top-level members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

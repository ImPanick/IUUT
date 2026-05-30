using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// <c>Profile.json</c> — account-wide meta that survives character deletion
/// (field guide §3, master doc §8.2). Properties are declared in the game's field
/// order so serialized output matches the original layout. Unknown top-level
/// members round-trip verbatim via <see cref="AdditionalData"/> (CONSTITUTION VI).
/// </summary>
public sealed class ProfileModel
{
    /// <summary>The Steam ID. Must equal the parent folder name; never edited by IUUT.</summary>
    [JsonPropertyName("UserID")]
    public string UserId { get; set; } = "";

    /// <summary>Account-wide currencies (Credits, exotics, Biomass, …).</summary>
    public List<MetaResource> MetaResources { get; set; } = [];

    /// <summary>Account-level unlock bits (DLC, mission-chain, tutorial gates).</summary>
    public List<int> UnlockedFlags { get; set; } = [];

    /// <summary>Workshop/blueprint unlocks (<c>Workshop_*</c>, <c>Prospect_*</c>), Rank always 1.</summary>
    public List<Talent> Talents { get; set; } = [];

    /// <summary>Slot index assigned to the next character created. Read-only display in IUUT.</summary>
    public int NextChrSlot { get; set; }

    /// <summary>Schema version (currently 4 — Mendel). Preserved verbatim; never edited.</summary>
    public int DataVersion { get; set; }

    /// <summary>Unknown top-level members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

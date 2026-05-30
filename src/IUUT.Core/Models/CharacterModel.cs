using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// One character record (field guide §4). In <c>Characters.json</c> each record is
/// stored as a JSON-*stringified* element of the nested container — see
/// <see cref="IUUT.Core.Io.NestedStringifiedJson"/>. Properties are declared in the
/// game's field order; unknown members round-trip verbatim via
/// <see cref="AdditionalData"/> (CONSTITUTION VI).
/// </summary>
public sealed class CharacterModel
{
    /// <summary>Display name. The only cosmetic-adjacent field IUUT lets the user edit.</summary>
    public string CharacterName { get; set; } = "";

    /// <summary>Stable slot identity (1..N); unique within the file, ≤ <c>Profile.NextChrSlot - 1</c>.</summary>
    public int ChrSlot { get; set; }

    /// <summary>Total accumulated experience.</summary>
    public long XP { get; set; }

    /// <summary>XP penalty pool subtracted before crediting new XP. Set 0 to clear debt.</summary>
    public long XP_Debt { get; set; }

    /// <summary>Permadeath flag. Set false to revive.</summary>
    public bool IsDead { get; set; }

    /// <summary>Abandoned-in-prospect (insurance-loss) state.</summary>
    public bool IsAbandoned { get; set; }

    /// <summary>Prospect this character was last in. Read-only display.</summary>
    public string LastProspectId { get; set; } = "";

    /// <summary>Last terrain key (e.g. <c>Outpost006_Olympus</c>). Read-only display.</summary>
    public string Location { get; set; } = "";

    /// <summary>Character-level unlock bits (separate from the account flags in Profile.json).</summary>
    public List<int> UnlockedFlags { get; set; } = [];

    /// <summary>Per-character carry-over currencies (usually empty).</summary>
    public List<MetaResource> MetaResources { get; set; } = [];

    /// <summary>Appearance indices (read-only in IUUT). See <see cref="CosmeticModel"/>.</summary>
    public CosmeticModel Cosmetic { get; set; } = new();

    /// <summary>Gameplay skill-tree entries (Rank 0–4). Shares the <see cref="Talent"/> shape.</summary>
    public List<Talent> Talents { get; set; } = [];

    /// <summary>Unix epoch seconds this character was last played. Read-only; preserve verbatim.</summary>
    public long TimeLastPlayed { get; set; }

    /// <summary>Unknown top-level members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

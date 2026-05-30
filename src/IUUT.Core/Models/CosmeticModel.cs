using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// A character's appearance block (field guide §4.3, verified against the live save).
/// **Every value is an integer index** into an in-game cosmetic table, except
/// <see cref="IsMale"/>. There are no hex/RGBA colour strings — colours are integer
/// palette indices. IUUT treats this as read-only (cosmetics are editable in-game).
/// </summary>
public sealed class CosmeticModel
{
    /// <summary>Head/face mesh index.</summary>
    public int Customization_Head { get; set; }

    /// <summary>Hair style index.</summary>
    public int Customization_Hair { get; set; }

    /// <summary>Hair colour palette index (not a hex string).</summary>
    public int Customization_HairColor { get; set; }

    /// <summary>Body type index.</summary>
    public int Customization_Body { get; set; }

    /// <summary>Body/clothing colour palette index.</summary>
    public int Customization_BodyColor { get; set; }

    /// <summary>Skin tone palette index.</summary>
    public int Customization_SkinTone { get; set; }

    /// <summary>Face tattoo index (0 = none).</summary>
    public int Customization_HeadTattoo { get; set; }

    /// <summary>Face scar index (0 = none).</summary>
    public int Customization_HeadScar { get; set; }

    /// <summary>Facial-hair index (0 = none).</summary>
    public int Customization_HeadFacialHair { get; set; }

    /// <summary>Cap/helmet logo index.</summary>
    public int Customization_CapLogo { get; set; }

    /// <summary>Body/voice base.</summary>
    public bool IsMale { get; set; }

    /// <summary>Voice index.</summary>
    public int Customization_Voice { get; set; }

    /// <summary>Eye colour palette index.</summary>
    public int Customization_EyeColor { get; set; }

    /// <summary>Unknown members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

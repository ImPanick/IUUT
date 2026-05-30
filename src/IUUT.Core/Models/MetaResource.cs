using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// One account currency entry in <see cref="ProfileModel.MetaResources"/>
/// (field guide §3.2). The game's loader is a <c>Map&lt;MetaRow, int&gt;</c>;
/// one entry per <see cref="MetaRow"/>.
/// </summary>
public sealed class MetaResource
{
    /// <summary>The currency key, e.g. <c>Credits</c>, <c>Exotic_Red</c>, <c>Biomass</c>.</summary>
    public string MetaRow { get; set; } = "";

    /// <summary>The amount. Unbounded; the game clamps to its own cap on load.</summary>
    public long Count { get; set; }

    /// <summary>Unknown members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

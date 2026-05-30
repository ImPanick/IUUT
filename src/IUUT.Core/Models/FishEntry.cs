using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// One fish-tracking record in <see cref="BestiaryModel.FishTracking"/> (verified on
/// the live save: <c>{ FishRow, MaxQuality, MaxWeight, MaxLength, CaughtCount }</c>).
/// </summary>
public sealed class FishEntry
{
    /// <summary>The fish's data-table reference.</summary>
    public DataTableRef FishRow { get; set; } = new();

    /// <summary>Best quality caught.</summary>
    public long MaxQuality { get; set; }

    /// <summary>Best weight caught.</summary>
    public long MaxWeight { get; set; }

    /// <summary>Best length caught.</summary>
    public long MaxLength { get; set; }

    /// <summary>Total number caught.</summary>
    public long CaughtCount { get; set; }

    /// <summary>Unknown members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

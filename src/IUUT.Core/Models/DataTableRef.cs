using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Models;

/// <summary>
/// A <c>{ RowName, DataTableName }</c> reference into a game data table. Used by
/// accolades (<c>D_Accolades</c>), bestiary groups (<c>D_BestiaryData</c>), and fish
/// rows — the shape is identical, so the model is shared.
/// </summary>
public sealed class DataTableRef
{
    /// <summary>The row key within the data table.</summary>
    public string RowName { get; set; } = "";

    /// <summary>The data table name, e.g. <c>D_Accolades</c>.</summary>
    public string DataTableName { get; set; } = "";

    /// <summary>Unknown members preserved verbatim on round-trip (CONSTITUTION VI).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}

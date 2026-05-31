using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Catalog;

/// <summary>
/// One row in a catalog table. <see cref="RowName"/> is always present; richer fields
/// (<see cref="DisplayName"/>, and table-specific data like max-rank or durability in
/// <see cref="Extra"/>) are populated as the catalog is enriched (master doc §15).
/// </summary>
public sealed class CatalogRow
{
    /// <summary>The data-table row key, e.g. <c>Workshop_Envirosuit</c>.</summary>
    public string RowName { get; set; } = "";

    /// <summary>Human-readable name, or <c>null</c> until enriched.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Any additional table-specific fields (max rank, tree, durability …), preserved verbatim.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extra { get; set; }

    /// <summary>The best label for the UI: <see cref="DisplayName"/> if present, else <see cref="RowName"/>.</summary>
    [JsonIgnore]
    public string Label => string.IsNullOrEmpty(DisplayName) ? RowName : DisplayName;
}

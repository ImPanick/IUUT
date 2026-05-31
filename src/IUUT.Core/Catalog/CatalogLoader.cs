using System.Text.Json;
using System.Text.Json.Serialization;
using IUUT.Catalog;

namespace IUUT.Core.Catalog;

/// <summary>Loads <see cref="CatalogTable"/>s from catalog JSON (streams or embedded resources).</summary>
public static class CatalogLoader
{
    // Catalog files are IUUT's own (camelCase); case-insensitive matching keeps the
    // loader decoupled from the game-save JSON options.
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Loads a catalog table from a JSON stream.</summary>
    /// <exception cref="JsonException">The stream is not a valid catalog file.</exception>
    public static CatalogTable Load(Stream json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var file = JsonSerializer.Deserialize<CatalogFile>(json, _options)
            ?? throw new JsonException("Catalog file deserialized to null.");
        return new CatalogTable(file.DataTable, file.CatalogVersion, file.Rows);
    }

    /// <summary>Loads an embedded catalog file shipped in <c>IUUT.Catalog</c> (e.g. <c>talents.json</c>).</summary>
    public static CatalogTable LoadEmbedded(string fileName)
    {
        using var stream = CatalogResources.Open(fileName);
        return Load(stream);
    }

    private sealed class CatalogFile
    {
        public string CatalogVersion { get; set; } = "";
        public string DataTable { get; set; } = "";
        public string? Source { get; set; }

        [JsonPropertyName("rows")]
        public List<CatalogRow> Rows { get; set; } = [];
    }
}

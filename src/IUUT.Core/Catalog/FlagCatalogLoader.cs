using System.Text.Json;
using System.Text.Json.Serialization;
using IUUT.Catalog;

namespace IUUT.Core.Catalog;

/// <summary>Loads <see cref="FlagCatalog"/>s from the embedded <c>accountflags.json</c> / <c>characterflags.json</c> data.</summary>
public static class FlagCatalogLoader
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Loads a flag catalog from a JSON stream.</summary>
    /// <exception cref="JsonException">The stream is not a valid flag catalog.</exception>
    public static FlagCatalog Load(Stream json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var file = JsonSerializer.Deserialize<FlagFile>(json, _options)
            ?? throw new JsonException("Flag catalog deserialized to null.");
        return new FlagCatalog(file.RowStruct, file.Names);
    }

    /// <summary>Loads an embedded flag catalog shipped in <c>IUUT.Catalog</c>.</summary>
    public static FlagCatalog LoadEmbedded(string fileName)
    {
        using var stream = CatalogResources.Open(fileName);
        return Load(stream);
    }

    private sealed class FlagFile
    {
        public string RowStruct { get; set; } = "";

        [JsonPropertyName("names")]
        public List<string> Names { get; set; } = [];
    }
}

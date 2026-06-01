using System.Text.Json;
using System.Text.Json.Serialization;
using IUUT.Catalog;

namespace IUUT.Core.Catalog;

/// <summary>Loads the <see cref="MissionCatalog"/> from the embedded <c>missions.json</c> (mission graph).</summary>
public static class MissionCatalogLoader
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Loads the mission catalog from a JSON stream.</summary>
    /// <exception cref="JsonException">The stream is not a valid mission catalog.</exception>
    public static MissionCatalog Load(Stream json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var file = JsonSerializer.Deserialize<MissionFile>(json, _options)
            ?? throw new JsonException("Mission catalog deserialized to null.");
        return new MissionCatalog(file.Missions.Select(m =>
            new MissionCatalog.MissionNode(m.RowName, m.Tree, m.Requires, m.DefaultUnlocked)));
    }

    /// <summary>Loads the embedded <c>missions.json</c> shipped in <c>IUUT.Catalog</c>.</summary>
    public static MissionCatalog LoadEmbedded(string fileName)
    {
        using var stream = CatalogResources.Open(fileName);
        return Load(stream);
    }

    private sealed class MissionFile
    {
        [JsonPropertyName("missions")]
        public List<MissionEntry> Missions { get; set; } = [];
    }

    private sealed class MissionEntry
    {
        public string RowName { get; set; } = "";
        public string? Tree { get; set; }
        public List<string> Requires { get; set; } = [];
        public bool DefaultUnlocked { get; set; }
    }
}

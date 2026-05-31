using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Parsers;

/// <summary>Parses a <c>Prospects\&lt;name&gt;.json</c> world save into a <see cref="ProspectFileModel"/> (blob preserved verbatim, CONSTITUTION VI).</summary>
public static class ProspectFileParser
{
    /// <summary>Deserializes a prospect world-save file.</summary>
    /// <exception cref="System.Text.Json.JsonException">The content is not valid prospect JSON.</exception>
    public static ProspectFileModel Parse(string json) => IcarusJson.Deserialize<ProspectFileModel>(json);
}

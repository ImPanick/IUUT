using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Parsers;

/// <summary>Parses <c>Mounts.json</c> into a <see cref="MountsModel"/> (RecorderBlob preserved verbatim, CONSTITUTION VI).</summary>
public static class MountsParser
{
    /// <summary>Deserializes <c>Mounts.json</c> content.</summary>
    /// <exception cref="System.Text.Json.JsonException">The content is not valid Mounts JSON.</exception>
    public static MountsModel Parse(string json) => IcarusJson.Deserialize<MountsModel>(json);
}

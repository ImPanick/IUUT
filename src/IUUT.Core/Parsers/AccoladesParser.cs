using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Parsers;

/// <summary>Parses <c>Accolades.json</c> into an <see cref="AccoladesModel"/> (unknowns preserved, CONSTITUTION VI).</summary>
public static class AccoladesParser
{
    /// <summary>Deserializes <c>Accolades.json</c> content.</summary>
    /// <exception cref="System.Text.Json.JsonException">The content is not valid Accolades JSON.</exception>
    public static AccoladesModel Parse(string json) => IcarusJson.Deserialize<AccoladesModel>(json);
}

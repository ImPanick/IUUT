using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Parsers;

/// <summary>Parses <c>BestiaryData.json</c> into a <see cref="BestiaryModel"/> (unknowns preserved, CONSTITUTION VI).</summary>
public static class BestiaryParser
{
    /// <summary>Deserializes <c>BestiaryData.json</c> content.</summary>
    /// <exception cref="System.Text.Json.JsonException">The content is not valid Bestiary JSON.</exception>
    public static BestiaryModel Parse(string json) => IcarusJson.Deserialize<BestiaryModel>(json);
}

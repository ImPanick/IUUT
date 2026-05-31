using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Parsers;

/// <summary>Parses <c>Loadout\Loadouts.json</c> into a <see cref="LoadoutsModel"/> (unknowns preserved, CONSTITUTION VI).</summary>
public static class LoadoutsParser
{
    /// <summary>Deserializes <c>Loadouts.json</c> content.</summary>
    /// <exception cref="System.Text.Json.JsonException">The content is not valid Loadouts JSON.</exception>
    public static LoadoutsModel Parse(string json) => IcarusJson.Deserialize<LoadoutsModel>(json);
}

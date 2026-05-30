using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Parsers;

/// <summary>
/// Parses <c>Characters.json</c> — the nested-stringified container (field guide §4) —
/// into the character roster. Unknown members are preserved (CONSTITUTION VI).
/// </summary>
public static class CharactersParser
{
    /// <summary>The single outer key of the container (literally the file name).</summary>
    public const string ContainerKey = "Characters.json";

    /// <summary>Deserializes the roster from <c>Characters.json</c> content.</summary>
    /// <exception cref="System.Text.Json.JsonException">The content is not a valid Characters container.</exception>
    public static List<CharacterModel> Parse(string json) =>
        NestedStringifiedJson.Parse<CharacterModel>(json, ContainerKey);
}

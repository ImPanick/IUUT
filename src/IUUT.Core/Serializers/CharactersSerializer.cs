using IUUT.Core.Io;
using IUUT.Core.Models;
using IUUT.Core.Parsers;

namespace IUUT.Core.Serializers;

/// <summary>
/// Serializes the character roster back into the <c>Characters.json</c>
/// nested-stringified container. The on-disk write (UTF-8 without BOM) is performed
/// by <c>SafeSaveWriter</c>, not here.
/// </summary>
public static class CharactersSerializer
{
    /// <summary>Serializes <paramref name="characters"/> to <c>Characters.json</c> text.</summary>
    public static string Serialize(IEnumerable<CharacterModel> characters)
    {
        ArgumentNullException.ThrowIfNull(characters);
        return NestedStringifiedJson.Serialize(CharactersParser.ContainerKey, characters);
    }
}

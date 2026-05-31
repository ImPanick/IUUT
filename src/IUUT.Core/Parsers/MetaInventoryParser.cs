using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Parsers;

/// <summary>Parses <c>MetaInventory.json</c> into a <see cref="MetaInventoryModel"/> (unknowns preserved, CONSTITUTION VI).</summary>
public static class MetaInventoryParser
{
    /// <summary>Deserializes <c>MetaInventory.json</c> content.</summary>
    /// <exception cref="System.Text.Json.JsonException">The content is not valid MetaInventory JSON.</exception>
    public static MetaInventoryModel Parse(string json) => IcarusJson.Deserialize<MetaInventoryModel>(json);
}

using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Serializers;

/// <summary>Serializes a <see cref="MetaInventoryModel"/> to <c>MetaInventory.json</c> text (on-disk write is SafeSaveWriter's job).</summary>
public static class MetaInventorySerializer
{
    /// <summary>Serializes <paramref name="inventory"/> to JSON text.</summary>
    public static string Serialize(MetaInventoryModel inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        return IcarusJson.Serialize(inventory);
    }
}

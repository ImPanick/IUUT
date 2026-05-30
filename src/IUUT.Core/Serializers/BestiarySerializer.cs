using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Serializers;

/// <summary>Serializes a <see cref="BestiaryModel"/> to <c>BestiaryData.json</c> text (on-disk write is SafeSaveWriter's job).</summary>
public static class BestiarySerializer
{
    /// <summary>Serializes <paramref name="bestiary"/> to JSON text.</summary>
    public static string Serialize(BestiaryModel bestiary)
    {
        ArgumentNullException.ThrowIfNull(bestiary);
        return IcarusJson.Serialize(bestiary);
    }
}

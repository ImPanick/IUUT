using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Serializers;

/// <summary>Serializes a <see cref="ProspectFileModel"/> back to prospect JSON (on-disk write is SafeSaveWriter's job).</summary>
public static class ProspectFileSerializer
{
    /// <summary>Serializes <paramref name="prospect"/> to JSON text.</summary>
    public static string Serialize(ProspectFileModel prospect)
    {
        ArgumentNullException.ThrowIfNull(prospect);
        return IcarusJson.Serialize(prospect);
    }
}

using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Serializers;

/// <summary>Serializes a <see cref="MountsModel"/> to <c>Mounts.json</c> text (on-disk write is SafeSaveWriter's job).</summary>
public static class MountsSerializer
{
    /// <summary>Serializes <paramref name="mounts"/> to JSON text.</summary>
    public static string Serialize(MountsModel mounts)
    {
        ArgumentNullException.ThrowIfNull(mounts);
        return IcarusJson.Serialize(mounts);
    }
}

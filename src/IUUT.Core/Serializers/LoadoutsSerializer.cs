using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Serializers;

/// <summary>Serializes a <see cref="LoadoutsModel"/> to <c>Loadouts.json</c> text (on-disk write is SafeSaveWriter's job).</summary>
public static class LoadoutsSerializer
{
    /// <summary>Serializes <paramref name="loadouts"/> to JSON text.</summary>
    public static string Serialize(LoadoutsModel loadouts)
    {
        ArgumentNullException.ThrowIfNull(loadouts);
        return IcarusJson.Serialize(loadouts);
    }
}

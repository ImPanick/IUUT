using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Serializers;

/// <summary>Serializes an <see cref="AccoladesModel"/> to <c>Accolades.json</c> text (on-disk write is SafeSaveWriter's job).</summary>
public static class AccoladesSerializer
{
    /// <summary>Serializes <paramref name="accolades"/> to JSON text.</summary>
    public static string Serialize(AccoladesModel accolades)
    {
        ArgumentNullException.ThrowIfNull(accolades);
        return IcarusJson.Serialize(accolades);
    }
}

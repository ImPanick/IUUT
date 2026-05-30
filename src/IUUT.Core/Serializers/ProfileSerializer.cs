using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Serializers;

/// <summary>
/// Serializes a <see cref="ProfileModel"/> back to <c>Profile.json</c> text using
/// the shared <see cref="IcarusJson"/> options. The on-disk write (UTF-8 without
/// BOM) is performed by <c>SafeSaveWriter</c>, not here.
/// </summary>
public static class ProfileSerializer
{
    /// <summary>Serializes <paramref name="profile"/> to JSON text.</summary>
    public static string Serialize(ProfileModel profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return IcarusJson.Serialize(profile);
    }
}

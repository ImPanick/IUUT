using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Parsers;

/// <summary>
/// Parses <c>Profile.json</c> content into a <see cref="ProfileModel"/>. Pure
/// deserialization — structural validation is the <c>ValidationEngine</c>'s job.
/// Unknown members are preserved (CONSTITUTION VI) by the model's extension data.
/// </summary>
public static class ProfileParser
{
    /// <summary>
    /// Deserializes <paramref name="json"/> into a <see cref="ProfileModel"/>.
    /// </summary>
    /// <exception cref="System.Text.Json.JsonException">The content is not valid Profile JSON.</exception>
    public static ProfileModel Parse(string json) => IcarusJson.Deserialize<ProfileModel>(json);
}

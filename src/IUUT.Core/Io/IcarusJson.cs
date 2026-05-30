using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IUUT.Core.Io;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> and helpers for reading/writing Icarus
/// save JSON. Centralised so every parser and serializer behaves identically:
/// indented output (spaces are accepted by the game — master doc §7.7), relaxed
/// escaping so characters such as <c>&amp;</c> in names survive, and unknown
/// members preserved by the models' own <c>[JsonExtensionData]</c> (CONSTITUTION VI).
/// </summary>
/// <remarks>
/// Writing UTF-8 <em>without</em> BOM is enforced by <see cref="SafeSaveWriter"/>,
/// which owns the file write. This type only does in-memory (de)serialization.
/// </remarks>
public static class IcarusJson
{
    /// <summary>The canonical options used for all Icarus save JSON.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions() => new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>Serializes <paramref name="value"/> to a JSON string.</summary>
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>
    /// Deserializes <paramref name="json"/> to <typeparamref name="T"/>. Throws
    /// <see cref="JsonException"/> if the payload deserializes to <c>null</c>.
    /// </summary>
    public static T Deserialize<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var result = JsonSerializer.Deserialize<T>(json, Options);
        return result is null
            ? throw new JsonException($"Deserialized JSON to null for type {typeof(T).Name}.")
            : result;
    }
}

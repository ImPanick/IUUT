using System.Text.Json;
using System.Text.Json.Nodes;

namespace IUUT.Core.Io;

/// <summary>
/// Reads and writes Icarus's "nested stringified array" containers: an outer JSON
/// object with a single key (the file name) whose value is an array of
/// JSON-<em>stringified</em> objects. Used by <c>Characters.json</c> and
/// <c>AssociatedProspects_Slot_N.json</c> (field guide §4, §7).
/// </summary>
/// <remarks>
/// Shape: <c>{ "&lt;key&gt;": [ "{\"A\":1,...}", "{\"A\":2,...}" ] }</c> — each array
/// element is a string whose content is itself the JSON of one inner object.
/// </remarks>
public static class NestedStringifiedJson
{
    /// <summary>
    /// Parses the array of stringified objects found under <paramref name="key"/> into
    /// a list of <typeparamref name="T"/>. Unknown members within each inner object are
    /// preserved by that model's extension data (CONSTITUTION VI).
    /// </summary>
    /// <exception cref="JsonException">The key is missing, not an array, or an element is not a JSON string.</exception>
    public static List<T> Parse<T>(string outerJson, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(outerJson);
        ArgumentException.ThrowIfNullOrEmpty(key);

        using var doc = JsonDocument.Parse(outerJson);
        if (!doc.RootElement.TryGetProperty(key, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Expected an array under key '{key}'.");
        }

        var items = new List<T>(array.GetArrayLength());
        foreach (var element in array.EnumerateArray())
        {
            if (element.ValueKind != JsonValueKind.String)
            {
                throw new JsonException($"Element under '{key}' is {element.ValueKind}, expected a stringified-JSON string.");
            }

            items.Add(IcarusJson.Deserialize<T>(element.GetString()!));
        }

        return items;
    }

    /// <summary>
    /// Serializes <paramref name="items"/> into the nested container shape — each item
    /// becomes a JSON string element of the array under <paramref name="key"/>.
    /// </summary>
    public static string Serialize<T>(string key, IEnumerable<T> items)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(items);

        var array = new JsonArray();
        foreach (var item in items)
        {
            array.Add(JsonValue.Create(IcarusJson.Serialize(item)));
        }

        var outer = new JsonObject { [key] = array };
        return outer.ToJsonString(IcarusJson.Options);
    }
}

using System.Text.Json;
using IUUT.Core.Io;
using IUUT.Core.Models;

namespace IUUT.Core.Parsers;

/// <summary>
/// Parses <c>AssociatedProspects_Slot_N.json</c> — the nested-stringified container (master §8.8).
/// The outer key (the file name) varies per slot, so it is auto-detected and preserved.
/// </summary>
public static class AssociatedProspectsParser
{
    /// <summary>Deserializes the container, capturing its outer key for an exact round-trip.</summary>
    /// <exception cref="JsonException">The content is not a single-key nested-stringified container.</exception>
    public static AssociatedProspectsModel Parse(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("AssociatedProspects must be a JSON object with a single container key.");
        }

        string? key = null;
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            key = property.Name;
            break;
        }

        if (key is null)
        {
            throw new JsonException("AssociatedProspects container has no outer key.");
        }

        return new AssociatedProspectsModel
        {
            ContainerKey = key,
            Prospects = NestedStringifiedJson.Parse<AssociatedProspect>(json, key),
        };
    }
}

namespace IUUT.Core.Prospects.World;

/// <summary>
/// Resolves the character names a prospect remembers for each player, so a rescue can say
/// "Wren" instead of "player …9282".
/// <para>
/// Every world carries a <c>PlayerHistoryRecorderComponent</c> whose <c>SavedHistoryData</c> pairs
/// a <c>UserID</c> (the SteamID) with the <c>CachedCharacterName</c> the game last saw for them.
/// That is the only place a world stores anything human-readable about who was in it.
/// </para>
/// <para>
/// These names are the user's own data and belong on their screen. They are PII and must never be
/// committed, logged to a shared location, or used in a test fixture (CONSTITUTION VII).
/// </para>
/// </summary>
public static class ProspectPlayerNames
{
    /// <summary>
    /// Maps SteamID to the last character name the world saw for it. Empty when the world has no
    /// history recorder — callers should fall back to a masked id rather than inventing a name.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Read(byte[] decompressed)
    {
        ArgumentNullException.ThrowIfNull(decompressed);

        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null)
        {
            return names;
        }

        foreach (var actor in recorders.Children)
        {
            var componentClass = ProspectCharacterReader.FindString(actor, decompressed, "ComponentClassName") ?? "";
            if (!componentClass.Contains("PlayerHistory", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Each history entry carries its own UserID + CachedCharacterName pair; walking the
            // entries (rather than the whole actor) is what keeps them correctly paired.
            foreach (var entry in FindAll(actor, "SavedHistoryData"))
            {
                var id = ProspectCharacterReader.FindString(entry, decompressed, "UserID");
                var name = ProspectCharacterReader.FindString(entry, decompressed, "CachedCharacterName");
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name) && name != "None")
                {
                    names[id] = name;
                }
            }
        }

        return names;
    }

    /// <summary>
    /// A display label for one player: their character name when the world remembers one, else the
    /// masked id. Never returns a raw SteamID.
    /// </summary>
    public static string Describe(IReadOnlyDictionary<string, string> names, ProspectCharacter character)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(character);

        return names.TryGetValue(character.PlayerId, out var name) && name.Length > 0
            ? name
            : $"Player {character.MaskedPlayerId}";
    }

    // The leaf entries of a named array — each one a separate record, not a flattened bag.
    private static IEnumerable<UeProperty> FindAll(UeProperty node, string name)
    {
        if (string.Equals(node.Name, name, StringComparison.Ordinal) &&
            string.Equals(node.Type, "ArrayProperty", StringComparison.Ordinal))
        {
            foreach (var child in node.Children)
            {
                yield return child;
            }

            yield break;
        }

        foreach (var child in node.Children)
        {
            foreach (var found in FindAll(child, name))
            {
                yield return found;
            }
        }
    }
}

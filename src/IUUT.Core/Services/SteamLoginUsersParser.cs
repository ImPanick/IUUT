using IUUT.Core.Io;

namespace IUUT.Core.Services;

/// <summary>
/// Extracts <c>SteamID64 → PersonaName</c> from a Steam <c>loginusers.vdf</c>
/// (master doc §7.5.1). The file's root <c>"users"</c> object maps each account's
/// SteamID64 to a block containing <c>PersonaName</c>.
/// </summary>
public static class SteamLoginUsersParser
{
    /// <summary>Parses <c>loginusers.vdf</c> content into a SteamID64 → PersonaName map.</summary>
    public static IReadOnlyDictionary<string, string> Parse(string vdfContent)
    {
        ArgumentNullException.ThrowIfNull(vdfContent);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var root = Vdf.Parse(vdfContent);

        if (!root.TryGetObject("users", out var users))
        {
            return result;
        }

        foreach (var (steamId, accountNode) in users.Children)
        {
            if (accountNode.IsObject && accountNode.TryGetString("PersonaName", out var name) && name.Length > 0)
            {
                result[steamId] = name;
            }
        }

        return result;
    }
}

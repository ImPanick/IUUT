using System.Text.Json;
using IUUT.Core.Parsers;

namespace IUUT.Core.Services;

/// <summary>
/// Discovers Icarus save profiles: resolves the <c>Saved\</c> root and enumerates the
/// SteamID64 folders under <c>PlayerData\</c>, attaching lightweight metadata
/// (character count, last-modified, presence) per master doc §7.1 and F-001..F-005.
/// PersonaName resolution is a separate concern (WP-6).
/// </summary>
public sealed class SaveDiscoveryService
{
    /// <summary>The folder under the Saved root that holds per-account save folders.</summary>
    public const string PlayerDataFolder = "PlayerData";

    /// <summary>
    /// The default save root: <c>%LOCALAPPDATA%\Icarus\Saved</c>. Never hardcodes a
    /// username (master doc §7.1). The UI lets the user override this when it does not
    /// exist (manual Browse fallback).
    /// </summary>
    public static string ResolveDefaultSaveRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Icarus",
            "Saved");

    /// <summary>Whether <paramref name="saveRoot"/> contains a <c>PlayerData\</c> folder.</summary>
    public bool SaveRootContainsPlayerData(string saveRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveRoot);
        return Directory.Exists(Path.Combine(saveRoot, PlayerDataFolder));
    }

    /// <summary>
    /// Enumerates profile folders under <c>&lt;saveRoot&gt;\PlayerData</c>, ordered by folder
    /// name. Returns an empty list if the <c>PlayerData</c> folder does not exist (the caller
    /// then prompts for a manual root — master doc §7.1).
    /// </summary>
    public IReadOnlyList<SaveProfile> DiscoverProfiles(string saveRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(saveRoot);

        var playerData = Path.Combine(saveRoot, PlayerDataFolder);
        if (!Directory.Exists(playerData))
        {
            return [];
        }

        var profiles = new List<SaveProfile>();
        foreach (var dir in Directory.EnumerateDirectories(playerData))
        {
            profiles.Add(BuildProfile(dir));
        }

        return profiles
            .OrderBy(p => p.SteamId64, StringComparer.Ordinal)
            .ToList();
    }

    private static SaveProfile BuildProfile(string folderPath)
    {
        var id = Path.GetFileName(folderPath);

        return new SaveProfile
        {
            SteamId64 = id,
            FolderPath = folderPath,
            CharacterCount = TryCountCharacters(folderPath),
            LastModifiedUtc = TryGetLastModifiedUtc(folderPath),
            HasProfileJson = File.Exists(Path.Combine(folderPath, "Profile.json")),
            LooksLikeSteamId64 = id.Length == 17 && id.All(char.IsAsciiDigit),
        };
    }

    private static int? TryCountCharacters(string folderPath)
    {
        var path = Path.Combine(folderPath, "Characters.json");
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return CharactersParser.Parse(File.ReadAllText(path)).Count;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static DateTimeOffset? TryGetLastModifiedUtc(string folderPath)
    {
        try
        {
            DateTime? max = null;
            foreach (var file in Directory.EnumerateFiles(folderPath))
            {
                var modified = File.GetLastWriteTimeUtc(file);
                if (max is null || modified > max)
                {
                    max = modified;
                }
            }

            return max is null ? null : new DateTimeOffset(max.Value, TimeSpan.Zero);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}

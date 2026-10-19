using IUUT.Core.DataPak;

namespace IUUT.Core.Services;

/// <summary>Where a location came from — shown to the user so they know whether to trust it.</summary>
public enum LocationSource
{
    /// <summary>The user chose it.</summary>
    Manual,

    /// <summary>The standard location (the Steam install, or <c>%LOCALAPPDATA%\Icarus\Saved</c>).</summary>
    Default,

    /// <summary>Found in another Steam library listed in <c>libraryfolders.vdf</c>.</summary>
    SteamLibrary,

    /// <summary>Found inside an Icarus Dedicated Server install.</summary>
    DedicatedServer,

    /// <summary>Not found anywhere IUUT looked.</summary>
    NotFound,
}

/// <summary>A resolved location, or the honest absence of one, with a plain-English explanation.</summary>
public sealed record LocatedPath(string? Path, LocationSource Source, string Detail)
{
    /// <summary>Whether a usable path was found.</summary>
    public bool Found => Path is not null;
}

/// <summary>
/// Finds the game install and the save folder — automatically where possible, from the user's
/// own choice where not — and tells the user which happened.
/// <para>
/// Auto-detection covers the default Steam install, every library in Steam's
/// <c>libraryfolders.vdf</c> (so a game moved to another drive is still found), the standard
/// <c>%LOCALAPPDATA%\Icarus\Saved</c>, and Icarus Dedicated Server installs in those libraries.
/// A manual choice always wins, and is forgiving: the user can pick the folder they think of as
/// "the game" or "the saves" (the Steam library, the Icarus folder, PlayerData, one profile, a
/// Prospects folder, or the pak itself) and it is normalised to what IUUT needs. A manual choice
/// that no longer exists is reported, not silently swapped for an auto-detected one.
/// </para>
/// <para>Read-only discovery: no writes, no network (CONSTITUTION V).</para>
/// </summary>
public static class GameLocations
{
    /// <summary>The Icarus Dedicated Server's Steam install folder name.</summary>
    public const string DedicatedServerFolder = "Icarus Dedicated Server";

    private const int MaxClimb = 5;

    /// <summary>Finds the game's <c>data.pak</c>.</summary>
    public static LocatedPath FindGame(string? manualOverride, string? steamRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(manualOverride))
        {
            return NormalizeGamePick(manualOverride) is { } pak
                ? new LocatedPath(pak, LocationSource.Manual, "Set by you.")
                : new LocatedPath(null, LocationSource.NotFound,
                    $"The game folder you chose ({manualOverride}) no longer contains Icarus. Browse to it again, or switch back to automatic.");
        }

        var libraries = DataPakLocator.Libraries(steamRoot);
        for (var i = 0; i < libraries.Count; i++)
        {
            var pak = Path.Combine(libraries[i], DataPakLocator.RelativePakPath);
            if (File.Exists(pak))
            {
                return i == 0
                    ? new LocatedPath(pak, LocationSource.Default, "Found automatically in your Steam install.")
                    : new LocatedPath(pak, LocationSource.SteamLibrary, $"Found automatically in your Steam library on {libraries[i]}.");
            }
        }

        return new LocatedPath(null, LocationSource.NotFound,
            $"Icarus wasn't found in any of your {libraries.Count} Steam librar{(libraries.Count == 1 ? "y" : "ies")}. Browse to where it is installed.");
    }

    /// <summary>Finds the <c>Saved</c> folder (the one containing <c>PlayerData</c>).</summary>
    public static LocatedPath FindSaves(string? manualOverride, string? steamRoot = null, string? localAppData = null)
    {
        if (!string.IsNullOrWhiteSpace(manualOverride))
        {
            return NormalizeSavePick(manualOverride) is { } saved
                ? new LocatedPath(saved, LocationSource.Manual, "Set by you.")
                : new LocatedPath(null, LocationSource.NotFound,
                    $"The save folder you chose ({manualOverride}) no longer contains Icarus saves. Browse to it again, or switch back to automatic.");
        }

        var appData = string.IsNullOrWhiteSpace(localAppData)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : localAppData;
        var standard = Path.Combine(appData, "Icarus", "Saved");
        if (HasPlayerData(standard))
        {
            return new LocatedPath(standard, LocationSource.Default, "Found automatically in the standard location.");
        }

        foreach (var library in DataPakLocator.Libraries(steamRoot))
        {
            var server = Path.Combine(library, "steamapps", "common", DedicatedServerFolder, "Icarus", "Saved");
            if (HasPlayerData(server))
            {
                return new LocatedPath(server, LocationSource.DedicatedServer,
                    $"Found automatically in an Icarus Dedicated Server install on {library}.");
            }
        }

        return new LocatedPath(null, LocationSource.NotFound,
            "No Icarus saves were found. If you play on another PC or a rented server, copy its Saved folder here and Browse to it.");
    }

    /// <summary>
    /// Turns whatever folder (or file) the user picked into the game's <c>data.pak</c>, or null
    /// when Icarus isn't there. Accepts the pak itself, any folder inside the install, the install,
    /// <c>steamapps\common</c>, or a whole Steam library.
    /// </summary>
    public static string? NormalizeGamePick(string picked)
    {
        if (string.IsNullOrWhiteSpace(picked))
        {
            return null;
        }

        if (File.Exists(picked))
        {
            return string.Equals(Path.GetFileName(picked), "data.pak", StringComparison.OrdinalIgnoreCase) ? picked : null;
        }

        string[] relatives =
        [
            "data.pak",
            Path.Combine("Data", "data.pak"),
            Path.Combine("Content", "Data", "data.pak"),
            Path.Combine("Icarus", "Content", "Data", "data.pak"),
            Path.Combine("Icarus", "Icarus", "Content", "Data", "data.pak"),
            Path.Combine("common", "Icarus", "Icarus", "Content", "Data", "data.pak"),
            DataPakLocator.RelativePakPath,
        ];

        foreach (var dir in SelfAndInstallAncestors(picked))
        {
            foreach (var relative in relatives)
            {
                var candidate = Path.Combine(dir, relative);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Turns whatever folder the user picked into the <c>Saved</c> folder, or null when no Icarus
    /// saves are there. Accepts <c>Saved</c> itself, <c>PlayerData</c>, one profile, a
    /// <c>Prospects</c> folder, or a folder that contains <c>Saved</c> / <c>Icarus\Saved</c>.
    /// </summary>
    public static string? NormalizeSavePick(string picked)
    {
        if (string.IsNullOrWhiteSpace(picked) || !Directory.Exists(picked))
        {
            return null;
        }

        DirectoryInfo dir;
        try
        {
            dir = new DirectoryInfo(Path.GetFullPath(picked));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        // Down: the pick itself, or a folder that contains Saved / Icarus\Saved.
        foreach (var candidate in new[] { dir.FullName, Path.Combine(dir.FullName, "Saved"), Path.Combine(dir.FullName, "Icarus", "Saved") })
        {
            if (HasPlayerData(candidate))
            {
                return candidate;
            }
        }

        // Up: ONLY through the save structure itself — PlayerData, a profile, its Prospects.
        // Climbing through arbitrary ancestors is how a random folder under AppData\Local
        // silently resolved to the real %LOCALAPPDATA%\Icarus\Saved (caught by a test).
        DirectoryInfo? saved = null;
        if (Is(dir, SaveDiscoveryService.PlayerDataFolder))
        {
            saved = dir.Parent;
        }
        else if (dir.Parent is { } parent && Is(parent, SaveDiscoveryService.PlayerDataFolder))
        {
            saved = parent.Parent;
        }
        else if (Is(dir, "Prospects") && dir.Parent?.Parent is { } playerData && Is(playerData, SaveDiscoveryService.PlayerDataFolder))
        {
            saved = playerData.Parent;
        }

        return saved is not null && HasPlayerData(saved.FullName) ? saved.FullName : null;
    }

    private static bool HasPlayerData(string savedFolder) =>
        Directory.Exists(Path.Combine(savedFolder, SaveDiscoveryService.PlayerDataFolder));

    private static bool Is(DirectoryInfo dir, string name) =>
        string.Equals(dir.Name, name, StringComparison.OrdinalIgnoreCase);

    // The pick, then its parents — but only while still inside an Icarus install, so a stray pick
    // cannot climb out and land on some other copy of the game.
    private static IEnumerable<string> SelfAndInstallAncestors(string path)
    {
        DirectoryInfo? dir;
        try
        {
            dir = new DirectoryInfo(Path.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            yield break;
        }

        yield return dir.FullName;

        string[] insideInstall = ["Data", "Content", "Icarus", "Paks", "Binaries", "Win64", "Config"];
        for (var i = 0; i < MaxClimb && dir.Parent is not null && insideInstall.Contains(dir.Name, StringComparer.OrdinalIgnoreCase); i++)
        {
            dir = dir.Parent;
            yield return dir.FullName;
        }
    }
}

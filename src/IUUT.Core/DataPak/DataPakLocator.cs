using IUUT.Core.Io;

namespace IUUT.Core.DataPak;

/// <summary>
/// Finds the installed game's <c>data.pak</c> for the runtime catalog self-refresh (roadmap Tier 1):
/// an explicit override first, then the default Steam install, then every library listed in Steam's
/// <c>libraryfolders.vdf</c> (parsed with the existing <see cref="Vdf"/> reader). Read-only discovery;
/// no filesystem writes, no network (CONSTITUTION V).
/// </summary>
public static class DataPakLocator
{
    /// <summary>The pak path relative to a Steam library root.</summary>
    public const string RelativePakPath = @"steamapps\common\Icarus\Icarus\Content\Data\data.pak";

    /// <summary>
    /// Resolves the first existing <c>data.pak</c>: <paramref name="overridePath"/> (if given), the
    /// Steam root's own install, then each <c>libraryfolders.vdf</c> library. <c>null</c> when none found.
    /// </summary>
    public static string? Resolve(string? overridePath = null, string? steamRoot = null)
    {
        foreach (var candidate in Candidates(overridePath, steamRoot))
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>Every candidate path in probe order (override → Steam root → VDF libraries), unfiltered.
    /// <paramref name="steamRoot"/> defaults to the machine's Program Files Steam (overridable for tests).</summary>
    public static IEnumerable<string> Candidates(string? overridePath = null, string? steamRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            yield return overridePath;
        }

        var root = string.IsNullOrWhiteSpace(steamRoot) ? DefaultSteamRoot() : steamRoot;
        yield return Path.Combine(root, RelativePakPath);

        foreach (var library in LibrariesFrom(Path.Combine(root, "steamapps", "libraryfolders.vdf")))
        {
            yield return Path.Combine(library, RelativePakPath);
        }
    }

    private static string DefaultSteamRoot()
    {
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        return string.IsNullOrEmpty(programFilesX86)
            ? @"C:\Program Files (x86)\Steam"
            : Path.Combine(programFilesX86, "Steam");
    }

    // libraryfolders.vdf: { "libraryfolders" { "0" { "path" "D:\\SteamLibrary" } "1" { … } } }
    private static List<string> LibrariesFrom(string vdfPath)
    {
        VdfNode root;
        try
        {
            if (!File.Exists(vdfPath))
            {
                return [];
            }

            root = Vdf.Parse(File.ReadAllText(vdfPath));
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }

        if (!root.TryGetObject("libraryfolders", out var folders))
        {
            return [];
        }

        var paths = new List<string>();
        foreach (var child in folders.Children.Values)
        {
            if (child.IsObject && child.TryGetString("path", out var path) && !string.IsNullOrWhiteSpace(path))
            {
                paths.Add(path);
            }
        }

        return paths;
    }
}

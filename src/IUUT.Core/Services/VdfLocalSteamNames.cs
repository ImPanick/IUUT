namespace IUUT.Core.Services;

/// <summary>
/// <see cref="ILocalSteamNames"/> backed by the local Steam <c>loginusers.vdf</c>
/// (master doc §7.5.1). The file is loaded once, lazily.
/// </summary>
/// <remarks>
/// IUUT.Core stays cross-platform and dependency-free, so this uses **common-path**
/// discovery (no Windows registry). The WPF app — which targets net8.0-windows — may
/// resolve the precise path from <c>HKCU\Software\Valve\Steam → SteamPath</c> and pass
/// it to the <see cref="VdfLocalSteamNames(string?)"/> constructor for non-default installs.
/// </remarks>
public sealed class VdfLocalSteamNames : ILocalSteamNames
{
    private static readonly IReadOnlyDictionary<string, string> _empty =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private readonly Lazy<IReadOnlyDictionary<string, string>> _names;

    /// <summary>Discovers <c>loginusers.vdf</c> via common Steam install paths.</summary>
    public VdfLocalSteamNames()
        : this(FindDefaultLoginUsersPath())
    {
    }

    /// <summary>Uses the given <c>loginusers.vdf</c> path (e.g. a registry-resolved path from the app).</summary>
    public VdfLocalSteamNames(string? loginUsersVdfPath) =>
        _names = new Lazy<IReadOnlyDictionary<string, string>>(() => Load(loginUsersVdfPath));

    /// <inheritdoc />
    public string? TryGetPersonaName(string steamId64) =>
        _names.Value.TryGetValue(steamId64, out var name) ? name : null;

    private static IReadOnlyDictionary<string, string> Load(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return _empty;
        }

        try
        {
            return SteamLoginUsersParser.Parse(File.ReadAllText(path));
        }
        catch (IOException)
        {
            return _empty;
        }
        catch (UnauthorizedAccessException)
        {
            return _empty;
        }
    }

    private static string? FindDefaultLoginUsersPath()
    {
        foreach (var root in CommonSteamRoots())
        {
            var candidate = Path.Combine(root, "config", "loginusers.vdf");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> CommonSteamRoots()
    {
        var programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        if (!string.IsNullOrEmpty(programFilesX86))
        {
            yield return Path.Combine(programFilesX86, "Steam");
        }

        var programFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        if (!string.IsNullOrEmpty(programFiles))
        {
            yield return Path.Combine(programFiles, "Steam");
        }

        yield return @"C:\Program Files (x86)\Steam";
        yield return @"C:\Steam";
    }
}

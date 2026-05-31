using System.Text;
using System.Text.Json;
using IUUT.Core.Io;

namespace IUUT.Core.Services;

/// <summary>
/// Loads/saves the <see cref="SteamProfileCache"/> to disk
/// (<c>%AppData%\IUUT\steam-profile-cache.json</c>, master doc §7.5.1). Resilient: a
/// missing or unreadable cache file yields a fresh empty cache rather than throwing.
/// </summary>
public static class SteamProfileCacheStore
{
    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Loads the cache from <paramref name="path"/>, or returns an empty cache if absent/corrupt.</summary>
    public static SteamProfileCache Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
        {
            return new SteamProfileCache();
        }

        try
        {
            return IcarusJson.Deserialize<SteamProfileCache>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return new SteamProfileCache();
        }
        catch (IOException)
        {
            return new SteamProfileCache();
        }
    }

    /// <summary>Saves <paramref name="cache"/> to <paramref name="path"/> (UTF-8, no BOM), creating the folder.</summary>
    public static void Save(string path, SteamProfileCache cache)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(cache);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, IcarusJson.Serialize(cache), _utf8NoBom);
    }
}

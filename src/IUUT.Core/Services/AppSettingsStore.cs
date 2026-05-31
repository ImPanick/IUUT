using System.Text;
using System.Text.Json;
using IUUT.Core.Io;

namespace IUUT.Core.Services;

/// <summary>
/// Loads/saves <see cref="AppSettings"/> to <c>&lt;StateRoot&gt;\settings.json</c>.
/// Resilient: a missing or unreadable file yields defaults rather than throwing.
/// </summary>
public static class AppSettingsStore
{
    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Loads settings from <paramref name="path"/>, or returns defaults if absent/corrupt.</summary>
    public static AppSettings Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
        {
            return new AppSettings();
        }

        try
        {
            return IcarusJson.Deserialize<AppSettings>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }

    /// <summary>Saves <paramref name="settings"/> to <paramref name="path"/> (UTF-8, no BOM), creating the folder.</summary>
    public static void Save(string path, AppSettings settings)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(settings);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, IcarusJson.Serialize(settings), _utf8NoBom);
    }
}

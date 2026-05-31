namespace IUUT.Core.Services;

/// <summary>
/// Resolves IUUT's single on-disk state folder and the paths within it, honoring the
/// footprint guarantee (master doc §6.4): <b>default</b> = <c>%AppData%\IUUT\</c>;
/// <b>portable</b> = <c>.\IUUT-Data\</c> beside the exe when an <c>IUUT.portable</c>
/// marker file is present. Removal is deleting the exe plus this one folder.
/// </summary>
public sealed class AppPaths
{
    /// <summary>Default state folder name under <c>%AppData%</c>.</summary>
    public const string AppDataFolderName = "IUUT";

    /// <summary>Marker file (beside the exe) that activates portable mode.</summary>
    public const string PortableMarkerFileName = "IUUT.portable";

    /// <summary>Portable state folder name (beside the exe).</summary>
    public const string PortableDataFolderName = "IUUT-Data";

    /// <summary>Steam name-cache file name.</summary>
    public const string SteamCacheFileName = "steam-profile-cache.json";

    /// <summary>Settings file name.</summary>
    public const string SettingsFileName = "settings.json";

    /// <summary>DPAPI-encrypted Steam API key file name (written by the app layer).</summary>
    public const string ApiKeyFileName = "steam-api-key.bin";

    /// <summary>Logs subfolder name.</summary>
    public const string LogsFolderName = "Logs";

    /// <summary>Single-file native-extraction subfolder name (see remarks).</summary>
    public const string RuntimeFolderName = "runtime";

    private AppPaths(bool isPortable, string stateRoot)
    {
        IsPortable = isPortable;
        StateRoot = stateRoot;
    }

    /// <summary>Whether portable mode is active.</summary>
    public bool IsPortable { get; }

    /// <summary>The one folder that holds all IUUT state.</summary>
    public string StateRoot { get; }

    /// <summary>Path to the Steam name cache.</summary>
    public string SteamCacheFile => Path.Combine(StateRoot, SteamCacheFileName);

    /// <summary>Path to the settings file.</summary>
    public string SettingsFile => Path.Combine(StateRoot, SettingsFileName);

    /// <summary>Path to the DPAPI-encrypted API key blob.</summary>
    public string ApiKeyFile => Path.Combine(StateRoot, ApiKeyFileName);

    /// <summary>The logs directory.</summary>
    public string LogsDirectory => Path.Combine(StateRoot, LogsFolderName);

    /// <summary>
    /// The directory the single-file host should extract native libs to, so extraction
    /// stays inside the one footprint. The app sets <c>DOTNET_BUNDLE_EXTRACT_BASE_DIR</c>
    /// to this at startup (it must be set before the bundle extracts).
    /// </summary>
    public string RuntimeExtractDirectory => Path.Combine(StateRoot, RuntimeFolderName);

    /// <summary>Resolves paths from the running exe's directory and the user's roaming AppData.</summary>
    public static AppPaths Resolve() =>
        Resolve(AppContext.BaseDirectory, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    /// <summary>Resolves paths from explicit roots (used by tests). Portable wins when the marker exists.</summary>
    public static AppPaths Resolve(string executableDirectory, string appDataRoot)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableDirectory);
        ArgumentException.ThrowIfNullOrEmpty(appDataRoot);

        var isPortable = File.Exists(Path.Combine(executableDirectory, PortableMarkerFileName));
        var stateRoot = isPortable
            ? Path.Combine(executableDirectory, PortableDataFolderName)
            : Path.Combine(appDataRoot, AppDataFolderName);

        return new AppPaths(isPortable, stateRoot);
    }

    /// <summary>Creates <see cref="StateRoot"/> if needed and returns it.</summary>
    public string EnsureStateRoot()
    {
        Directory.CreateDirectory(StateRoot);
        return StateRoot;
    }
}

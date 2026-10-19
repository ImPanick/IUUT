using System.IO;
using IUUT.Core.DataPak;
using IUUT.Core.Services;

namespace IUUT.App.Services;

/// <summary>
/// Where the game and the saves are — auto-detected, or set by the user — and remembered across
/// launches in <c>settings.json</c>.
/// <para>
/// Before this, the save folder chosen on Home lived only in memory (the persisted
/// <see cref="AppSettings.SaveRootOverride"/> existed but nothing loaded or wrote it), so every
/// restart forgot it; and the game had no manual fallback at all — if auto-detection missed it,
/// the catalog self-refresh simply never ran, with nothing on screen to say so.
/// </para>
/// </summary>
public sealed class LocationState
{
    private readonly AppPaths _paths;
    private readonly AppSettings _settings;

    /// <summary>Loads the remembered choices and resolves both locations.</summary>
    public LocationState(AppPaths paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        _paths = paths;
        _settings = AppSettingsStore.Load(paths.SettingsFile);
        Game = GameLocations.FindGame(_settings.GamePathOverride);
        Saves = GameLocations.FindSaves(_settings.SaveRootOverride);
    }

    /// <summary>The game's <c>data.pak</c>, and how it was found.</summary>
    public LocatedPath Game { get; private set; }

    /// <summary>The <c>Saved</c> folder, and how it was found.</summary>
    public LocatedPath Saves { get; private set; }

    /// <summary>
    /// Points IUUT at the game the user picked. Returns null on success, or a plain-English reason
    /// the folder was refused (nothing is remembered in that case).
    /// </summary>
    public string? ChooseGame(string picked)
    {
        if (GameLocations.NormalizeGamePick(picked) is not { } pak)
        {
            return $"Icarus wasn't found in {picked}. Pick the folder Icarus is installed in (or your Steam library).";
        }

        _settings.GamePathOverride = pak;
        Persist();
        Game = GameLocations.FindGame(pak);
        return null;
    }

    /// <summary>Forgets the manual game location and auto-detects again.</summary>
    public void AutoGame()
    {
        _settings.GamePathOverride = null;
        Persist();
        Game = GameLocations.FindGame(null);
    }

    /// <summary>
    /// Points IUUT at the saves the user picked, normalised to the <c>Saved</c> folder. Returns the
    /// normalised folder, or null when the pick holds no Icarus saves (nothing is remembered).
    /// </summary>
    public string? ChooseSaves(string picked)
    {
        if (GameLocations.NormalizeSavePick(picked) is not { } saved)
        {
            return null;
        }

        Remember(saved);
        return saved;
    }

    /// <summary>
    /// Records a save folder that was confirmed to work (e.g. typed on Home and loaded). If it is
    /// the one auto-detection finds anyway, the manual override is cleared rather than pinned.
    /// </summary>
    public void Remember(string savedFolder)
    {
        if (string.IsNullOrWhiteSpace(savedFolder))
        {
            return;
        }

        var auto = GameLocations.FindSaves(null);
        var isAuto = auto.Path is not null &&
                     string.Equals(Path.GetFullPath(auto.Path), Path.GetFullPath(savedFolder), StringComparison.OrdinalIgnoreCase);
        var desired = isAuto ? null : savedFolder;

        if (!string.Equals(_settings.SaveRootOverride, desired, StringComparison.OrdinalIgnoreCase))
        {
            _settings.SaveRootOverride = desired;
            Persist();
        }

        Saves = GameLocations.FindSaves(desired);
    }

    /// <summary>Forgets the manual save location and auto-detects again.</summary>
    public void AutoSaves()
    {
        _settings.SaveRootOverride = null;
        Persist();
        Saves = GameLocations.FindSaves(null);
    }

    /// <summary>
    /// Refreshes the catalog cache from the current game location (offline, sanity-gated). Returns
    /// a one-line outcome for the status bar. New catalogs take effect on the next launch.
    /// </summary>
    public string RefreshCatalogs()
    {
        if (Game.Path is not { } pak)
        {
            return "Game not located — catalogs stay on the shipped snapshot.";
        }

#pragma warning disable CA1031 // Best-effort: a failed refresh must leave the current catalogs in place.
        try
        {
            var result = new CatalogRefreshRunner(_paths).RefreshIfStale(pak);
            return result switch
            {
                null => "Catalogs already match this game build.",
                { Ok: true } => "Catalogs refreshed from this install — restart IUUT to use them.",
                _ => "This install's data didn't pass IUUT's safety checks; catalogs left unchanged.",
            };
        }
        catch (Exception ex)
        {
            return $"Catalog refresh failed ({ex.Message}); catalogs left unchanged.";
        }
#pragma warning restore CA1031
    }

    private void Persist()
    {
#pragma warning disable CA1031 // A read-only or locked settings file must not break location changes for this session.
        try
        {
            _paths.EnsureStateRoot();
            AppSettingsStore.Save(_paths.SettingsFile, _settings);
        }
        catch (Exception)
        {
            // The choice still applies for this session; it just won't survive a restart.
        }
#pragma warning restore CA1031
    }
}

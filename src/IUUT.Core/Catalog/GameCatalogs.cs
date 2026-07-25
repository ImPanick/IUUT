namespace IUUT.Core.Catalog;

/// <summary>
/// The full set of embedded game-data catalogs (master doc §15): talents, items,
/// accolades, bestiary, and meta-resources. Load once via <see cref="LoadEmbedded"/>
/// and share for the session.
/// </summary>
public sealed class GameCatalogs
{
    private GameCatalogs(
        CatalogTable talents,
        CatalogTable items,
        CatalogTable accolades,
        CatalogTable bestiary,
        CatalogTable metaResources,
        CatalogTable prospects,
        FlagCatalog accountFlags,
        FlagCatalog characterFlags,
        MissionCatalog missions)
    {
        Talents = talents;
        Items = items;
        Accolades = accolades;
        Bestiary = bestiary;
        MetaResources = metaResources;
        Prospects = prospects;
        AccountFlags = accountFlags;
        CharacterFlags = characterFlags;
        Missions = missions;
    }

    /// <summary><c>D_Talents</c> — character + workshop/prospect talents.</summary>
    public CatalogTable Talents { get; }

    /// <summary><c>D_ItemsStatic</c> — stash/loadout items.</summary>
    public CatalogTable Items { get; }

    /// <summary><c>D_Accolades</c> — accolades.</summary>
    public CatalogTable Accolades { get; }

    /// <summary><c>D_BestiaryData</c> — creature scan groups.</summary>
    public CatalogTable Bestiary { get; }

    /// <summary><c>D_MetaResources</c> — account currencies (with display names).</summary>
    public CatalogTable MetaResources { get; }

    /// <summary><c>D_ProspectList</c> — prospects with their in-game drop names (e.g. "ARCWOOD: Outpost").</summary>
    public CatalogTable Prospects { get; }

    /// <summary><c>D_AccountFlags</c> — <c>Profile.UnlockedFlags</c> ids (mission rewards, story grants, blueprints).</summary>
    public FlagCatalog AccountFlags { get; }

    /// <summary><c>D_CharacterFlags</c> — <c>flags_&lt;SteamID&gt;.dat</c> ids (talents, mission unlocks, map gates).</summary>
    public FlagCatalog CharacterFlags { get; }

    /// <summary>The mission graph (<c>Prospect_*</c> talents + their prerequisite DAG) for the Missions checklist.</summary>
    public MissionCatalog Missions { get; }

    /// <summary>Loads all catalogs from the embedded resources in <c>IUUT.Catalog</c>.</summary>
    public static GameCatalogs LoadEmbedded() => new(
        CatalogLoader.LoadEmbedded("talents.json"),
        CatalogLoader.LoadEmbedded("items.json"),
        CatalogLoader.LoadEmbedded("accolades.json"),
        CatalogLoader.LoadEmbedded("bestiary.json"),
        CatalogLoader.LoadEmbedded("metaresources.json"),
        CatalogLoader.LoadEmbedded("prospects.json"),
        FlagCatalogLoader.LoadEmbedded("accountflags.json"),
        FlagCatalogLoader.LoadEmbedded("characterflags.json"),
        MissionCatalogLoader.LoadEmbedded("missions.json"));

    /// <summary>
    /// Loads catalogs preferring the runtime self-refresh cache in <paramref name="cacheDirectory"/>
    /// (written by the catalog refresh runner from the user's own game data.pak), falling back to the
    /// embedded snapshot PER FILE when the cache file is missing or unreadable — a bad cache can
    /// never make the app worse than the shipped snapshot. metaresources.json is always embedded
    /// (curated whitelist; never regenerated).
    /// </summary>
    public static GameCatalogs LoadWithCache(string cacheDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(cacheDirectory);
        return new(
            Cached(cacheDirectory, "talents.json", CatalogLoader.Load, CatalogLoader.LoadEmbedded),
            Cached(cacheDirectory, "items.json", CatalogLoader.Load, CatalogLoader.LoadEmbedded),
            Cached(cacheDirectory, "accolades.json", CatalogLoader.Load, CatalogLoader.LoadEmbedded),
            Cached(cacheDirectory, "bestiary.json", CatalogLoader.Load, CatalogLoader.LoadEmbedded),
            CatalogLoader.LoadEmbedded("metaresources.json"),
            Cached(cacheDirectory, "prospects.json", CatalogLoader.Load, CatalogLoader.LoadEmbedded),
            Cached(cacheDirectory, "accountflags.json", FlagCatalogLoader.Load, FlagCatalogLoader.LoadEmbedded),
            Cached(cacheDirectory, "characterflags.json", FlagCatalogLoader.Load, FlagCatalogLoader.LoadEmbedded),
            Cached(cacheDirectory, "missions.json", MissionCatalogLoader.Load, MissionCatalogLoader.LoadEmbedded));
    }

    private static T Cached<T>(string cacheDirectory, string fileName, Func<Stream, T> load, Func<string, T> loadEmbedded)
    {
        var path = Path.Combine(cacheDirectory, fileName);
        if (File.Exists(path))
        {
            try
            {
                using var stream = File.OpenRead(path);
                return load(stream);
            }
#pragma warning disable CA1031 // Any cache defect falls back to the shipped snapshot by design.
            catch
            {
                // fall through to embedded
            }
#pragma warning restore CA1031
        }

        return loadEmbedded(fileName);
    }
}

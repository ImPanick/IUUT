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
        FlagCatalog accountFlags,
        FlagCatalog characterFlags)
    {
        Talents = talents;
        Items = items;
        Accolades = accolades;
        Bestiary = bestiary;
        MetaResources = metaResources;
        AccountFlags = accountFlags;
        CharacterFlags = characterFlags;
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

    /// <summary><c>D_AccountFlags</c> — <c>Profile.UnlockedFlags</c> ids (mission rewards, story grants, blueprints).</summary>
    public FlagCatalog AccountFlags { get; }

    /// <summary><c>D_CharacterFlags</c> — <c>flags_&lt;SteamID&gt;.dat</c> ids (talents, mission unlocks, map gates).</summary>
    public FlagCatalog CharacterFlags { get; }

    /// <summary>Loads all catalogs from the embedded resources in <c>IUUT.Catalog</c>.</summary>
    public static GameCatalogs LoadEmbedded() => new(
        CatalogLoader.LoadEmbedded("talents.json"),
        CatalogLoader.LoadEmbedded("items.json"),
        CatalogLoader.LoadEmbedded("accolades.json"),
        CatalogLoader.LoadEmbedded("bestiary.json"),
        CatalogLoader.LoadEmbedded("metaresources.json"),
        FlagCatalogLoader.LoadEmbedded("accountflags.json"),
        FlagCatalogLoader.LoadEmbedded("characterflags.json"));
}

namespace IUUT.Core.Editing;

/// <summary>One item carried by a loadout (an envirosuit or a meta item), by its <c>D_ItemsStatic</c>
/// RowName and how many of it the loadout holds.</summary>
public sealed record LoadoutItemRef(string RowName, int Count);

/// <summary>
/// A human-readable digest of one per-prospect loadout (master §8.7): which prospect/character it is
/// for and what it carries — derived from the loadout's <c>AssociatedProspect</c>, <c>EnviroSuit</c>
/// and <c>MetaItems</c> sub-blocks so the viewer can show names instead of raw GUIDs.
/// </summary>
public sealed record LoadoutSummary(
    int ChrSlot,
    string ProspectId,
    string ProspectState,
    string? EnviroSuitRowName,
    IReadOnlyList<LoadoutItemRef> Items,
    bool Insured,
    bool Settled,
    string LoadoutGuid);

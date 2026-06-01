namespace IUUT.Core.Catalog;

/// <summary>
/// An ordered flag table where the integer flag id is the 0-based row index into a data-mined
/// Icarus flag <c>GenerateEnum</c> DataTable (master §8.11). <c>AccountFlag</c> backs
/// <c>Profile.UnlockedFlags</c>; <c>CharacterFlag</c> backs <c>flags_&lt;SteamID&gt;.dat</c>. Provides
/// id↔name lookup, a humanized display label, and which ids represent mission/story completion.
/// Unknown ids (beyond the shipped snapshot) are tolerated — they label as <c>Flag N</c>.
/// </summary>
public sealed class FlagCatalog
{
    private readonly IReadOnlyList<string> _names;
    private readonly Dictionary<string, int> _idByName;

    /// <summary>Creates a flag catalog from the ordered names (index = flag id).</summary>
    public FlagCatalog(string rowStruct, IReadOnlyList<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        RowStruct = rowStruct ?? "";
        _names = names;
        _idByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < names.Count; i++)
        {
            _idByName.TryAdd(names[i], i);
        }
    }

    /// <summary>The source UE row-struct, e.g. <c>/Script/Icarus.CharacterFlag</c>.</summary>
    public string RowStruct { get; }

    /// <summary>Number of known flag rows in the shipped snapshot.</summary>
    public int Count => _names.Count;

    /// <summary>The raw flag name for <paramref name="id"/>, or <c>null</c> when out of the known range.</summary>
    public string? Name(int id) => id >= 0 && id < _names.Count ? _names[id] : null;

    /// <summary>A human-readable label for <paramref name="id"/> (humanized name, or <c>Flag N</c> if unknown).</summary>
    public string Label(int id) => Name(id) is { } name ? CatalogName.Humanize(name) : $"Flag {id}";

    /// <summary>Looks up the flag id for <paramref name="name"/>.</summary>
    public bool TryGetId(string name, out int id)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return _idByName.TryGetValue(name, out id);
    }

    /// <summary>All known flag ids in order.</summary>
    public IEnumerable<int> Ids => Enumerable.Range(0, _names.Count);

    /// <summary>The flag ids that represent mission / story completion (the "mark missions done" set).</summary>
    public IReadOnlyList<int> MissionFlagIds() => Ids.Where(IsMissionFlag).ToList();

    /// <summary>Whether <paramref name="id"/>'s flag represents mission / story completion.</summary>
    public bool IsMissionFlag(int id) => Name(id) is { } name && IsMissionOrStory(name);

    // Mission/story progression flags — Mission_* rewards, story-talent grants, map-unlock gates and
    // story level boosts. Excludes UI-state (Has_Seen_*), Test*, and pure blueprint grants.
    private static bool IsMissionOrStory(string name) =>
        name.StartsWith("Mission_", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Story", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Nightfall", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Ironclad", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Level_Boost", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith("_Unlock", StringComparison.OrdinalIgnoreCase);
}

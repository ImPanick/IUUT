namespace IUUT.Core.Catalog;

/// <summary>
/// The Icarus mission graph (master §8.8). A "mission" is a <c>Prospect_*</c> talent in
/// <c>Profile.Talents</c>; completing it = owning that talent. Prerequisites are the talent's
/// <c>RequiredTalents</c> (a verified DAG from <c>D_Talents</c>), so checking a mission complete must
/// also complete every transitive prerequisite. Grouped by region <see cref="MissionNode.Tree"/>.
/// Data: <c>missions.json</c> (re-mine per game version — see docs/DATA-PROVENANCE.md).
/// </summary>
public sealed class MissionCatalog
{
    /// <summary>One mission node: its reward talent, region tree, direct prerequisites, and whether it's a root.</summary>
    public sealed record MissionNode(string RowName, string? Tree, IReadOnlyList<string> Requires, bool DefaultUnlocked);

    private readonly Dictionary<string, MissionNode> _byRow;

    /// <summary>Creates the mission catalog from its nodes.</summary>
    public MissionCatalog(IEnumerable<MissionNode> missions)
    {
        ArgumentNullException.ThrowIfNull(missions);
        _byRow = missions
            .Where(m => !string.IsNullOrEmpty(m.RowName))
            .GroupBy(m => m.RowName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
    }

    /// <summary>Number of missions.</summary>
    public int Count => _byRow.Count;

    /// <summary>All mission nodes.</summary>
    public IReadOnlyCollection<MissionNode> Missions => _byRow.Values;

    /// <summary>Looks up a mission by its <c>Prospect_*</c> row name.</summary>
    public bool TryGet(string rowName, out MissionNode node) => _byRow.TryGetValue(rowName, out node!);

    /// <summary>A human-readable label: the mission name, derived from the RowName (Prospect_ prefix stripped).</summary>
    public static string Label(string rowName)
    {
        var name = rowName.StartsWith("Prospect_", StringComparison.Ordinal) ? rowName["Prospect_".Length..] : rowName;
        return CatalogName.Humanize(name);
    }

    /// <summary>The region tree label (e.g. "Olympus"), or "Other".</summary>
    public static string TreeLabel(string? tree)
    {
        if (string.IsNullOrEmpty(tree))
        {
            return "Other";
        }

        var name = tree.StartsWith("Prospect_", StringComparison.Ordinal) ? tree["Prospect_".Length..] : tree;
        return CatalogName.Humanize(name);
    }

    /// <summary>
    /// Every transitive prerequisite of <paramref name="rowName"/> (all ancestors via
    /// <c>Requires</c>), excluding the mission itself. Cycle-safe. These are the missions that must
    /// also be marked complete when completing this one.
    /// </summary>
    public IReadOnlyList<string> AllPrerequisites(string rowName)
    {
        ArgumentException.ThrowIfNullOrEmpty(rowName);
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        if (_byRow.TryGetValue(rowName, out var start))
        {
            foreach (var r in start.Requires)
            {
                stack.Push(r);
            }
        }

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!seen.Add(current) || string.Equals(current, rowName, StringComparison.Ordinal))
            {
                continue;
            }

            result.Add(current);
            if (_byRow.TryGetValue(current, out var node))
            {
                foreach (var r in node.Requires)
                {
                    stack.Push(r);
                }
            }
        }

        return result;
    }
}

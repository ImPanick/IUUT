using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>One player-built structure inside a prospect world blob.</summary>
public sealed record HomesteadActor(
    string RowName,
    string RecorderClass,
    int ActorGuid,
    int FoundationUid,
    int WhitelistedActorCount,
    ProspectTransform? Placement = null)
{
    /// <summary>Whether this piece is anchored to another actor (a foundation link that a move must preserve).</summary>
    public bool HasFoundation => FoundationUid >= 0;

    /// <summary>
    /// A display label: the item row when the recorder carries one, else the recorder kind
    /// (beds and building grids record no <c>StaticItemDataRowName</c>).
    /// </summary>
    public string Label => RowName.Length > 0
        ? RowName
        : RecorderClass.Replace("/Script/Icarus.", "", StringComparison.Ordinal)
                       .Replace("RecorderComponent", "", StringComparison.Ordinal);

    /// <summary>
    /// Whether this is a real placed structure that a relocation could move. The world's container
    /// registry (<c>ContainerManager</c>) classifies as player storage — correctly, because it holds
    /// the player's items and item rescue needs it — but it is a singleton sitting at the world
    /// origin, not something built on the ground. It stays in the survey and out of the builds.
    /// </summary>
    public bool IsPlaceable =>
        Placement is not null &&
        !RecorderClass.Contains("ContainerManager", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// One group of structures that sit together — a build. Players scatter deployables across a
/// map (beacons, remote mining rigs), so "the base" is a cluster, not the whole structure list.
/// </summary>
public sealed record HomesteadCluster(
    int Index,
    IReadOnlyList<HomesteadActor> Structures,
    double CentreX,
    double CentreY,
    double CentreZ,
    double SpanMetres)
{
    /// <summary>How many pieces this build contains.</summary>
    public int Count => Structures.Count;

    /// <summary>The most common structure kinds in this build, for identifying it at a glance.</summary>
    public IReadOnlyList<string> TopKinds =>
        Structures.GroupBy(s => s.Label, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Count()}x {g.Key}")
            .Take(3)
            .ToList();
}

/// <summary>
/// What a prospect's homestead consists of, and what moving it elsewhere would have to reconcile.
/// </summary>
public sealed record HomesteadSurvey(
    IReadOnlyList<HomesteadActor> Structures,
    int TotalActors,
    int DistinctActorGuids,
    int MaxActorGuid,
    IReadOnlyList<string> TileNames)
{
    /// <summary>Structures grouped by what they are, most numerous first.</summary>
    public IReadOnlyList<(string RowName, int Count)> ByKind =>
        Structures.GroupBy(s => s.Label, StringComparer.Ordinal)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(g => g.Item2)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

    /// <summary>Pieces anchored to a foundation — these carry cross-actor references.</summary>
    public int FoundationLinked => Structures.Count(s => s.HasFoundation);

    /// <summary>Pieces with a tame whitelist — another set of cross-actor references.</summary>
    public int WhitelistLinked => Structures.Count(s => s.WhitelistedActorCount > 0);

    /// <summary>The placements that decoded (structures whose world position is known).</summary>
    public IReadOnlyList<ProspectTransform> Placements =>
        Structures.Where(s => s.Placement is not null).Select(s => s.Placement!).ToList();

    /// <summary>
    /// Groups the placed structures into builds by single-link clustering: two pieces join the
    /// same build when they sit within <paramref name="radiusMetres"/> of each other. Ordered
    /// largest first, so index 0 is "the base". Structures whose placement did not decode are
    /// left out, as is the world container registry (see <see cref="HomesteadActor.IsPlaceable"/>) —
    /// neither can be positioned on the ground, so neither can be moved.
    /// </summary>
    public IReadOnlyList<HomesteadCluster> Clusters(double radiusMetres = 60)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusMetres);

        var placed = Structures.Where(s => s.IsPlaceable).ToList();
        var seen = new bool[placed.Count];
        var groups = new List<List<HomesteadActor>>();

        for (var i = 0; i < placed.Count; i++)
        {
            if (seen[i])
            {
                continue;
            }

            var group = new List<HomesteadActor>();
            var queue = new Queue<int>();
            queue.Enqueue(i);
            seen[i] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                group.Add(placed[current]);
                for (var j = 0; j < placed.Count; j++)
                {
                    if (!seen[j] && placed[current].Placement!.DistanceTo(placed[j].Placement!) <= radiusMetres)
                    {
                        seen[j] = true;
                        queue.Enqueue(j);
                    }
                }
            }

            groups.Add(group);
        }

        return groups
            .OrderByDescending(g => g.Count)
            .Select((g, index) =>
            {
                var points = g.Select(s => s.Placement!).ToList();
                var span = points.Count < 2 ? 0 : points.Max(a => points.Max(a.DistanceTo));
                return new HomesteadCluster(
                    index,
                    g,
                    points.Average(p => (double)p.X) / 100.0,
                    points.Average(p => (double)p.Y) / 100.0,
                    points.Average(p => (double)p.Z) / 100.0,
                    span);
            })
            .ToList();
    }

    /// <summary>
    /// The base's centre in metres and its longest span — the footprint a relocation would move.
    /// Null when no structure's placement decoded.
    /// </summary>
    public (double X, double Y, double Z, double SpanMetres)? Footprint
    {
        get
        {
            var placements = Placements;
            if (placements.Count == 0)
            {
                return null;
            }

            var cx = placements.Average(p => (double)p.X);
            var cy = placements.Average(p => (double)p.Y);
            var cz = placements.Average(p => (double)p.Z);
            var span = placements.Count < 2
                ? 0
                : placements.Max(a => placements.Max(a.DistanceTo));
            return (cx / 100.0, cy / 100.0, cz / 100.0, span);
        }
    }
}

/// <summary>
/// READ-ONLY survey of the player-built structures in a prospect world blob — the first
/// milestone of the homestead pack-up research track (roadmap rule: report before writes).
/// <para>
/// Beyond listing the base, this surfaces what a cross-prospect move would have to reconcile:
/// every actor's <c>IcarusActorGUID</c> (which must be remapped so it cannot collide with the
/// destination world's ids), <c>FoundationActorIcarusUID</c> and tame-whitelist links (which
/// point at other actors by id, so a partial move would dangle), and the terrain tiles the
/// world references (a base carries absolute coordinates, which mean something different on
/// another map). Nothing here mutates the blob.
/// </para>
/// </summary>
public sealed class ProspectHomesteadReader
{
    private const string BedClass = "Bed";
    private const string BuildingGridClass = "BuildingGrid";

    /// <summary>Decompresses a prospect blob and surveys its homestead.</summary>
    public HomesteadSurvey ReadBlob(ProspectBlobModel blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        return Read(ProspectBlobVerifier.Decompress(blob.BinaryBlob));
    }

    /// <summary>Surveys the homestead in an already-decompressed prospect world blob.</summary>
    public HomesteadSurvey Read(byte[] decompressed)
    {
        ArgumentNullException.ThrowIfNull(decompressed);

        var structures = new List<HomesteadActor>();
        var guids = new HashSet<int>();
        var tiles = new SortedSet<string>(StringComparer.Ordinal);
        var total = 0;

        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null)
        {
            return new HomesteadSurvey(structures, 0, 0, 0, []);
        }

        foreach (var actor in recorders.Children)
        {
            total++;
            var componentClass = FindString(actor, decompressed, "ComponentClassName") ?? "";

            if (FindInt(actor, decompressed, "IcarusActorGUID") is { } guid)
            {
                guids.Add(guid);
            }

            if (FindString(actor, decompressed, "TileName") is { Length: > 0 } tile &&
                !string.Equals(tile, "None", StringComparison.Ordinal))
            {
                tiles.Add(tile);
            }

            if (!IsPlayerBuilt(componentClass))
            {
                continue;
            }

            var transformNode = Find(actor, "ActorTransform");
            structures.Add(new HomesteadActor(
                FindString(actor, decompressed, "StaticItemDataRowName") ?? "",
                componentClass,
                FindInt(actor, decompressed, "IcarusActorGUID") ?? -1,
                FindInt(actor, decompressed, "FoundationActorIcarusUID") ?? -1,
                CountWhitelisted(actor),
                transformNode is null ? null : ProspectTransformReader.Read(decompressed, transformNode)));
        }

        return new HomesteadSurvey(structures, total, guids.Count, guids.Count == 0 ? 0 : guids.Max(), [.. tiles]);
    }

    /// <summary>
    /// Whether a recorder class represents something the player built (as opposed to world
    /// terrain, resource deposits, or creatures).
    /// </summary>
    public static bool IsPlayerBuilt(string? componentClass) =>
        SlotOwner.Classify(componentClass) == SlotOwnerKind.DeployedStorage ||
        (componentClass is not null &&
         (componentClass.Contains(BedClass, StringComparison.OrdinalIgnoreCase) ||
          componentClass.Contains(BuildingGridClass, StringComparison.OrdinalIgnoreCase)));

    private static int CountWhitelisted(UeProperty actor)
    {
        var node = Find(actor, "WhitelistedActors");
        return node?.Children.Count ?? 0;
    }

    private static UeProperty? Find(UeProperty node, string name)
    {
        if (string.Equals(node.Name, name, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            if (Find(child, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static string? FindString(UeProperty node, byte[] data, string name)
    {
        if (string.Equals(node.Name, name, StringComparison.Ordinal) && node.Type is "StrProperty" or "NameProperty")
        {
            var pos = node.ValueOffset;
            return UePropertyReader.ReadFString(data, ref pos);
        }

        foreach (var child in node.Children)
        {
            if (FindString(child, data, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private static int? FindInt(UeProperty node, byte[] data, string name)
    {
        if (string.Equals(node.Name, name, StringComparison.Ordinal) && node.Type is "IntProperty" or "UInt32Property")
        {
            return BitConverter.ToInt32(data, node.ValueOffset);
        }

        foreach (var child in node.Children)
        {
            if (FindInt(child, data, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}

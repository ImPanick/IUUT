using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>How much to trust an estimated ground height.</summary>
public enum TerrainHeightConfidence
{
    /// <summary>Samples are close and agree — safe to place on.</summary>
    High,

    /// <summary>Usable, but the ground is uneven or the samples are further out.</summary>
    Medium,

    /// <summary>A guess. Steep ground, a cave, or open country with nothing nearby to go on.</summary>
    Low,
}

/// <summary>
/// An estimated ground height at one spot, with everything a caller needs to decide whether to
/// trust it. Never present this as a survey — it is inferred, and <see cref="Confidence"/> is the
/// honest part.
/// </summary>
public sealed record TerrainHeightEstimate(
    double HeightMetres,
    TerrainHeightConfidence Confidence,
    double NearestSampleMetres,
    double NeighbourSpreadMetres,
    int SamplesUsed)
{
    /// <summary>Plain-English reason the confidence is what it is.</summary>
    public string Explanation => Confidence switch
    {
        TerrainHeightConfidence.High =>
            $"ground nearby is even (varies {NeighbourSpreadMetres:N0} m) and the closest reference point is {NearestSampleMetres:N0} m away",
        TerrainHeightConfidence.Medium when NearestSampleMetres >= MediumDistanceMetres =>
            $"the closest reference point is {NearestSampleMetres:N0} m away, so this is interpolated over open ground",
        TerrainHeightConfidence.Medium =>
            $"the ground around here varies by {NeighbourSpreadMetres:N0} m, so the estimate could be off by a few metres",
        _ when NearestSampleMetres >= LowDistanceMetres =>
            $"nothing within {LowDistanceMetres:N0} m to measure against — the closest is {NearestSampleMetres:N0} m away",
        _ => $"the ground here swings by {NeighbourSpreadMetres:N0} m — a cliff, a slope, or a cave mouth",
    };

    internal const double MediumDistanceMetres = 20;
    internal const double LowDistanceMetres = 60;
}

/// <summary>
/// A ground-height model of a prospect, built from the world's own actors.
/// <para>
/// Icarus ships real landscape heightmaps, but they are Oodle-compressed inside the game's
/// pakchunks and the game links Oodle statically, so there is no decompressor IUUT could reach
/// without bundling proprietary code. The save answers the question anyway: every actor IUUT does
/// not consider player-built — resource deposits, voxels, cave mouths — sits on the terrain, and
/// their placements decode. That is a scattered height field, free and offline.
/// </para>
/// <para>
/// Inverse-distance weighting over the <see cref="Neighbours"/> nearest samples. Two calibration
/// results decided this shape, both measured against 1,010 real placements whose true height is
/// known, across seven prospects:
/// </para>
/// <list type="bullet">
/// <item>Use EVERY world actor, not just resource deposits. Filtering to deposits looks purer and
/// is four times worse (median 8.3 m against 2.0 m) — density beats sample purity, because the
/// gaps it opens are wider than the error it removes.</item>
/// <item>Cave actors barely bias anything (−0.1 m to −5.2 m against the nearest deposit), so they
/// stay in. Excluding them moved the median by 0.03 m.</item>
/// </list>
/// <para>
/// Accuracy, same measurement: overall median 2.0 m, p90 8.5 m — but the tail reaches 191 m, and
/// that tail is what <see cref="TerrainHeightConfidence"/> exists to catch. Every catastrophic
/// miss falls in the low-confidence buckets. Restricted to High, 66% of placements are covered at
/// a median of 1.6 m, p90 4.4 m, worst 16.9 m.
/// </para>
/// </summary>
public sealed class TerrainHeightField
{
    /// <summary>How many nearby samples an estimate is drawn from.</summary>
    public const int Neighbours = 8;

    private const double HighSpreadMetres = 5;
    private const double LowSpreadMetres = 15;
    private const int MinimumSamples = 4;

    private readonly List<(double X, double Y, double Z)> _samples;

    private TerrainHeightField(List<(double X, double Y, double Z)> samples) => _samples = samples;

    /// <summary>How many ground samples this prospect yielded.</summary>
    public int SampleCount => _samples.Count;

    /// <summary>Builds a height field from a prospect file.</summary>
    public static TerrainHeightField FromProspect(ProspectFileModel prospect)
    {
        ArgumentNullException.ThrowIfNull(prospect);
        return FromBlob(ProspectBlobVerifier.Decompress(prospect.ProspectBlob.BinaryBlob));
    }

    /// <summary>Builds a height field from an already-decompressed prospect world blob.</summary>
    public static TerrainHeightField FromBlob(byte[] decompressed)
    {
        ArgumentNullException.ThrowIfNull(decompressed);

        var samples = new List<(double, double, double)>();
        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null)
        {
            return new TerrainHeightField(samples);
        }

        foreach (var actor in recorders.Children)
        {
            // Player-built pieces are excluded: they are what we are trying to place, and a base
            // built on stilts would teach the field its own mistake.
            if (ProspectHomesteadReader.IsPlayerBuilt(FindString(actor, decompressed, "ComponentClassName")))
            {
                continue;
            }

            var node = Find(actor, "ActorTransform");
            if (node is null || ProspectTransformReader.Read(decompressed, node) is not { } t)
            {
                continue;
            }

            // The world origin is where unplaced singletons sit, not a real ground sample.
            if (t.X == 0 && t.Y == 0 && t.Z == 0)
            {
                continue;
            }

            samples.Add((t.X / 100.0, t.Y / 100.0, t.Z / 100.0));
        }

        return new TerrainHeightField(samples);
    }

    /// <summary>
    /// Estimates the ground height at a point, in metres. Null when the prospect yielded too few
    /// samples to say anything at all.
    /// </summary>
    public TerrainHeightEstimate? EstimateAt(double xMetres, double yMetres)
    {
        if (_samples.Count < MinimumSamples)
        {
            return null;
        }

        var near = _samples
            .Select(s => (s, d: Math.Sqrt(((s.X - xMetres) * (s.X - xMetres)) + ((s.Y - yMetres) * (s.Y - yMetres)))))
            .OrderBy(t => t.d)
            .Take(Neighbours)
            .ToList();

        // Inverse-distance weighting, with a floor so a sample sitting exactly on the point
        // does not divide by zero and swamp the rest.
        var weightSum = near.Sum(n => 1.0 / Math.Max(n.d, 0.5));
        var height = near.Sum(n => n.s.Z / Math.Max(n.d, 0.5)) / weightSum;

        var mean = near.Average(n => n.s.Z);
        var spread = Math.Sqrt(near.Average(n => (n.s.Z - mean) * (n.s.Z - mean)));
        var nearest = near[0].d;

        var confidence =
            spread >= LowSpreadMetres || nearest >= TerrainHeightEstimate.LowDistanceMetres
                ? TerrainHeightConfidence.Low
                : spread < HighSpreadMetres && nearest < TerrainHeightEstimate.MediumDistanceMetres
                    ? TerrainHeightConfidence.High
                    : TerrainHeightConfidence.Medium;

        return new TerrainHeightEstimate(height, confidence, nearest, spread, near.Count);
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
}

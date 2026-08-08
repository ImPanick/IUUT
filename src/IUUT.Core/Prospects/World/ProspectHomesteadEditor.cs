using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>What a relocation changed.</summary>
public sealed record HomesteadMoveResult(int StructuresMoved, double OffsetX, double OffsetY, double OffsetZ)
{
    /// <summary>Whether anything actually moved.</summary>
    public bool Changed => StructuresMoved > 0;
}

/// <summary>
/// GATED WRITE: relocates a built cluster inside its own prospect by offsetting each structure's
/// <c>Translation</c> vector.
/// <para>
/// Every write is IN-PLACE and SIZE-PRESERVING — a translation is a fixed 12 bytes, so no tag is
/// rewritten and the blob's length never changes (the same low-risk class as the quest reset).
/// Three things that a cross-prospect move WOULD need are provably unnecessary here, which is why
/// this slice is safe: actor ids are untouched (same world, so no collisions and no dangling
/// <c>FoundationActorIcarusUID</c> links), tame whitelists are untouched, and player-built
/// structures carry no FLOD terrain binding (verified across every real prospect: <c>TileName</c>
/// is always <c>None</c> and the level/record/instance indices are always <c>-1</c>).
/// </para>
/// <para>
/// Relocation moves geometry only — it cannot know the destination's ground height, so a build
/// can end up floating or buried. Callers should say so before applying.
/// </para>
/// </summary>
public static class ProspectHomesteadEditor
{
    /// <summary>
    /// Offsets every structure in <paramref name="cluster"/> by the given metres, writing through
    /// <see cref="ProspectBlobCodec.SetUncompressed"/> when anything changed.
    /// </summary>
    public static HomesteadMoveResult MoveCluster(
        ProspectFileModel prospect, HomesteadCluster cluster, double offsetXMetres, double offsetYMetres, double offsetZMetres)
    {
        ArgumentNullException.ThrowIfNull(prospect);
        ArgumentNullException.ThrowIfNull(cluster);

        var data = ProspectBlobCodec.Decompress(prospect.ProspectBlob.BinaryBlob);
        var result = Move(data, cluster.Structures.Select(s => s.ActorGuid), offsetXMetres, offsetYMetres, offsetZMetres);
        if (result.Changed)
        {
            ProspectBlobCodec.SetUncompressed(prospect.ProspectBlob, data);
        }

        return result;
    }

    /// <summary>
    /// Offsets the structures whose <c>IcarusActorGUID</c> is in <paramref name="actorGuids"/>,
    /// mutating <paramref name="decompressed"/> in place. Offsets are metres; the save stores
    /// centimetres.
    /// </summary>
    public static HomesteadMoveResult Move(
        byte[] decompressed, IEnumerable<int> actorGuids, double offsetXMetres, double offsetYMetres, double offsetZMetres)
    {
        ArgumentNullException.ThrowIfNull(decompressed);
        ArgumentNullException.ThrowIfNull(actorGuids);

        var wanted = new HashSet<int>(actorGuids);
        if (wanted.Count == 0)
        {
            return new HomesteadMoveResult(0, offsetXMetres, offsetYMetres, offsetZMetres);
        }

        var dx = (float)(offsetXMetres * 100.0);
        var dy = (float)(offsetYMetres * 100.0);
        var dz = (float)(offsetZMetres * 100.0);
        if (dx == 0f && dy == 0f && dz == 0f)
        {
            return new HomesteadMoveResult(0, offsetXMetres, offsetYMetres, offsetZMetres);
        }

        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null)
        {
            return new HomesteadMoveResult(0, offsetXMetres, offsetYMetres, offsetZMetres);
        }

        var moved = 0;
        foreach (var actor in recorders.Children)
        {
            if (FindInt(actor, decompressed, "IcarusActorGUID") is not { } guid || !wanted.Contains(guid))
            {
                continue;
            }

            var transform = Find(actor, "ActorTransform");
            if (transform is null ||
                ProspectTransformReader.TranslationOffset(decompressed, transform) is not { } offset)
            {
                continue;
            }

            // In-place, size-preserving: three floats overwritten where they already sit.
            BitConverter.GetBytes(BitConverter.ToSingle(decompressed, offset) + dx).CopyTo(decompressed, offset);
            BitConverter.GetBytes(BitConverter.ToSingle(decompressed, offset + 4) + dy).CopyTo(decompressed, offset + 4);
            BitConverter.GetBytes(BitConverter.ToSingle(decompressed, offset + 8) + dz).CopyTo(decompressed, offset + 8);
            moved++;
        }

        return new HomesteadMoveResult(moved, offsetXMetres, offsetYMetres, offsetZMetres);
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

using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>What a character rescue changed.</summary>
public sealed record CharacterRescueResult(
    bool Moved,
    bool Revived,
    double ToXMetres,
    double ToYMetres,
    double ToZMetres)
{
    /// <summary>Whether anything was written.</summary>
    public bool Changed => Moved || Revived;
}

/// <summary>
/// GATED WRITE: moves a character recorded in a prospect world save, and optionally revives them.
/// <para>
/// This is the escape hatch for a trapped character — a zone that reset behind you, a boss that
/// glitched and pinned bodies somewhere unreachable, a spot the game will not let you return to.
/// The state lives only in the host's prospect save, so a host can free everyone from one file.
/// </para>
/// <para>
/// Both writes are IN-PLACE and SIZE-PRESERVING, the same low-risk class as the quest reset and the
/// base relocation. <c>Location</c> is a natively serialised <c>Vector</c> — three floats at the
/// value offset, no nested tags, so no length ever changes — and <c>bIsAlive</c>'s value byte lives
/// in the property TAG at <c>ValueOffset - 2</c>, which is where the writer puts it back.
/// </para>
/// <para>
/// The character's carried inventory is NOT touched: moving the body moves the items with it,
/// because they hang off the same recorder. That is the point — the gear comes home on the body.
/// </para>
/// </summary>
public static class ProspectCharacterEditor
{
    /// <summary>Health to restore when reviving, chosen to be survivable but not a free heal.</summary>
    public const int ReviveHealth = 50;

    /// <summary>
    /// Moves the character in <paramref name="prospect"/> to a world position in metres, writing
    /// through <see cref="ProspectBlobCodec.SetUncompressed"/> when anything changed.
    /// </summary>
    public static CharacterRescueResult Rescue(
        ProspectFileModel prospect,
        ProspectCharacter character,
        double toXMetres,
        double toYMetres,
        double toZMetres,
        bool revive = false)
    {
        ArgumentNullException.ThrowIfNull(prospect);

        var data = ProspectBlobCodec.Decompress(prospect.ProspectBlob.BinaryBlob);
        var result = Rescue(data, character, toXMetres, toYMetres, toZMetres, revive);
        if (result.Changed)
        {
            ProspectBlobCodec.SetUncompressed(prospect.ProspectBlob, data);
        }

        return result;
    }

    /// <summary>
    /// Moves the character to a world position in metres, mutating <paramref name="decompressed"/>
    /// in place. When <paramref name="revive"/> is set, also clears the death flag and restores
    /// enough health to stand up.
    /// </summary>
    public static CharacterRescueResult Rescue(
        byte[] decompressed,
        ProspectCharacter character,
        double toXMetres,
        double toYMetres,
        double toZMetres,
        bool revive = false)
    {
        ArgumentNullException.ThrowIfNull(decompressed);
        ArgumentNullException.ThrowIfNull(character);

        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null || character.RecorderIndex < 0 || character.RecorderIndex >= recorders.Children.Count)
        {
            return new CharacterRescueResult(false, false, toXMetres, toYMetres, toZMetres);
        }

        var actor = recorders.Children[character.RecorderIndex];

        // Guard against a stale index pointing at a different actor after an edit elsewhere.
        var playerId = ProspectCharacterReader.FindString(actor, decompressed, "PlayerID") ?? "";
        if (!string.Equals(playerId, character.PlayerId, StringComparison.Ordinal))
        {
            return new CharacterRescueResult(false, false, toXMetres, toYMetres, toZMetres);
        }

        var moved = false;
        var location = ProspectCharacterReader.Find(actor, "Location");
        if (location is not null && location.ValueOffset + 12 <= decompressed.Length)
        {
            // In-place, size-preserving: three floats overwritten where they already sit.
            BitConverter.GetBytes((float)(toXMetres * 100.0)).CopyTo(decompressed, location.ValueOffset);
            BitConverter.GetBytes((float)(toYMetres * 100.0)).CopyTo(decompressed, location.ValueOffset + 4);
            BitConverter.GetBytes((float)(toZMetres * 100.0)).CopyTo(decompressed, location.ValueOffset + 8);
            moved = true;
        }

        var revived = false;
        if (revive)
        {
            var alive = FindTyped(actor, "bIsAlive", "BoolProperty");
            if (alive is not null && alive.ValueOffset >= 2)
            {
                // A BoolProperty's value byte lives in the TAG, two bytes before the value span.
                decompressed[alive.ValueOffset - 2] = 1;
                revived = true;
            }

            var health = FindTyped(actor, "Health", "IntProperty");
            if (health is not null && health.ValueOffset + 4 <= decompressed.Length &&
                BitConverter.ToInt32(decompressed, health.ValueOffset) < ReviveHealth)
            {
                BitConverter.GetBytes(ReviveHealth).CopyTo(decompressed, health.ValueOffset);
            }
        }

        return new CharacterRescueResult(moved, revived, toXMetres, toYMetres, toZMetres);
    }

    private static UeProperty? FindTyped(UeProperty node, string name, string type)
    {
        if (string.Equals(node.Name, name, StringComparison.Ordinal) &&
            string.Equals(node.Type, type, StringComparison.Ordinal))
        {
            return node;
        }

        foreach (var child in node.Children)
        {
            if (FindTyped(child, name, type) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}

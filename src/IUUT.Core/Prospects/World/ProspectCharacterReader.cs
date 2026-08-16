using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>
/// One character recorded inside a prospect's world save — a player who dropped in and whose
/// position, vitals, and carried inventory the world is holding.
/// </summary>
/// <remarks>
/// <paramref name="PlayerId"/> is a real SteamID and is PII. Use <see cref="MaskedPlayerId"/> for
/// anything displayed, logged, or written to disk (CONSTITUTION VII).
/// </remarks>
public sealed record ProspectCharacter(
    string PlayerId,
    int CharacterSlot,
    bool IsAlive,
    int Health,
    int RespawnCount,
    ProspectTransform? Location,
    int CarriedSlots,
    int RecorderIndex)
{
    /// <summary>The last four digits only — enough to tell players apart, safe to display.</summary>
    public string MaskedPlayerId => PlayerId.Length >= 4 ? $"…{PlayerId[^4..]}" : "(unknown)";

    /// <summary>Whether this character is holding anything worth rescuing.</summary>
    public bool HasCarriedItems => CarriedSlots > 0;
}

/// <summary>
/// READ-ONLY listing of the characters inside a prospect world save.
/// <para>
/// This exists for the trapped-character case: a zone resets or a boss glitches, bodies end up
/// somewhere the player cannot reach or re-enter, and the game offers no way back to them. The
/// host holds the only copy of that state — it lives in the prospect's world save, not in any
/// player's own profile — so recovery has to happen here.
/// </para>
/// </summary>
public sealed class ProspectCharacterReader
{
    /// <summary>Decompresses a prospect blob and lists its characters.</summary>
    public IReadOnlyList<ProspectCharacter> ReadBlob(ProspectBlobModel blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        return Read(ProspectBlobVerifier.Decompress(blob.BinaryBlob));
    }

    /// <summary>Lists the characters in an already-decompressed prospect world blob.</summary>
    public IReadOnlyList<ProspectCharacter> Read(byte[] decompressed)
    {
        ArgumentNullException.ThrowIfNull(decompressed);

        var characters = new List<ProspectCharacter>();
        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null)
        {
            return characters;
        }

        for (var i = 0; i < recorders.Children.Count; i++)
        {
            var actor = recorders.Children[i];
            if (SlotOwner.Classify(FindString(actor, decompressed, "ComponentClassName")) != SlotOwnerKind.PlayerCarried)
            {
                continue;
            }

            // A container's slot list surfaces as BOTH an ArrayProperty and a StructProperty node
            // of the same name; count only the array or every slot is counted twice.
            var slots = 0;
            Walk(actor, n =>
            {
                if (string.Equals(n.Name, "Slots", StringComparison.Ordinal) &&
                    string.Equals(n.Type, "ArrayProperty", StringComparison.Ordinal))
                {
                    slots += n.Children.Count;
                }
            });

            characters.Add(new ProspectCharacter(
                FindString(actor, decompressed, "PlayerID") ?? "",
                FindInt(actor, decompressed, "ChrSlot") ?? -1,
                FindBool(actor, decompressed, "bIsAlive") ?? false,
                FindInt(actor, decompressed, "Health") ?? 0,
                FindInt(actor, decompressed, "RespawnCount") ?? 0,
                ReadLocation(actor, decompressed),
                slots,
                i));
        }

        return characters;
    }

    /// <summary>
    /// The character's saved world position. Unlike <c>ActorTransform</c>, <c>Location</c> is a
    /// natively serialised <c>Vector</c> — three floats at the value offset, no nested tags.
    /// </summary>
    internal static ProspectTransform? ReadLocation(UeProperty actor, byte[] data)
    {
        var node = Find(actor, "Location");
        if (node is null || node.ValueOffset + 12 > data.Length)
        {
            return null;
        }

        return new ProspectTransform(
            BitConverter.ToSingle(data, node.ValueOffset),
            BitConverter.ToSingle(data, node.ValueOffset + 4),
            BitConverter.ToSingle(data, node.ValueOffset + 8),
            0, 0, 0, 1, 1, 1, 1);
    }

    internal static void Walk(UeProperty node, Action<UeProperty> visit)
    {
        visit(node);
        foreach (var child in node.Children)
        {
            Walk(child, visit);
        }
    }

    internal static UeProperty? Find(UeProperty node, string name)
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

    internal static string? FindString(UeProperty node, byte[] data, string name)
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

    internal static int? FindInt(UeProperty node, byte[] data, string name)
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

    internal static bool? FindBool(UeProperty node, byte[] data, string name)
    {
        if (string.Equals(node.Name, name, StringComparison.Ordinal) && node.Type == "BoolProperty")
        {
            return UePropertyReader.ReadBool(data, node);
        }

        foreach (var child in node.Children)
        {
            if (FindBool(child, data, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}

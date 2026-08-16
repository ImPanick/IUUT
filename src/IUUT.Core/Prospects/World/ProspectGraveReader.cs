using IUUT.Core.Models;
using IUUT.Core.ProspectBlob;

namespace IUUT.Core.Prospects.World;

/// <summary>Which kind of gravestone a body left behind.</summary>
public enum GraveKind
{
    /// <summary>
    /// Down but not out — the body a teammate can still revive in-game
    /// (<c>Player_Gravestone_DBNO</c>).
    /// </summary>
    DownedBody,

    /// <summary>
    /// Missing in action (<c>Player_Gravestone_MIA</c>). The marker left when a player's body is
    /// gone or unreachable — a zone reset, a disconnect, a boss that pinned them somewhere the
    /// game will not let anyone return to. This is the one nobody can recover in-game.
    /// </summary>
    MissingInAction,
}

/// <summary>A player's gravestone and everything it is holding.</summary>
public sealed record ProspectGrave(
    GraveKind Kind,
    string RowName,
    int ActorGuid,
    ProspectTransform? Placement,
    int ItemSlots,
    int RecorderIndex)
{
    /// <summary>What to call this when telling someone their gear is recoverable.</summary>
    public string Label => Kind == GraveKind.MissingInAction ? "missing-in-action marker" : "downed body";

    /// <summary>Whether it is worth recovering.</summary>
    public bool HasItems => ItemSlots > 0;
}

/// <summary>
/// READ-ONLY: finds the gravestones in a prospect world save.
/// <para>
/// When a player dies or their body is stranded, the game leaves a gravestone actor holding their
/// inventory — the game's own data sizes it at 70 slots (<c>InventoryInfo.Player_Grave</c>) and
/// gives it <c>Loot_Grave</c> and <c>Revive_Grave</c> interactions. Two variants exist:
/// <c>Player_Gravestone_DBNO</c> for a body a teammate can still revive, and
/// <c>Player_Gravestone_MIA</c> for one that is simply gone.
/// </para>
/// <para>
/// A gravestone is a deployable container, so its contents are ALREADY swept by
/// <c>return-to-stash</c> — they classify as <see cref="SlotOwnerKind.DeployedStorage"/> along with
/// every crate and locker. What was missing is being able to tell one apart from the furniture:
/// with hundreds of storage slots in a world, "your body is here, holding this" is the difference
/// between a usable rescue and a haystack. That is all this adds.
/// </para>
/// <para>
/// NOT YET VERIFIED AGAINST A REAL GRAVESTONE — no save on hand contains one, because nobody in
/// them is dead. The row names and the 70-slot capacity come from the game's own tables, and the
/// recorder shape is the ordinary deployable shape, but the first real grave should be checked
/// before this is trusted blindly.
/// </para>
/// </summary>
public sealed class ProspectGraveReader
{
    /// <summary>The item row a downed, revivable body uses.</summary>
    public const string DownedRow = "Player_Gravestone_DBNO";

    /// <summary>The item row a missing-in-action marker uses.</summary>
    public const string MissingRow = "Player_Gravestone_MIA";

    /// <summary>Decompresses a prospect blob and finds its gravestones.</summary>
    public IReadOnlyList<ProspectGrave> ReadBlob(ProspectBlobModel blob)
    {
        ArgumentNullException.ThrowIfNull(blob);
        return Read(ProspectBlobVerifier.Decompress(blob.BinaryBlob));
    }

    /// <summary>Finds the gravestones in an already-decompressed prospect world blob.</summary>
    public IReadOnlyList<ProspectGrave> Read(byte[] decompressed)
    {
        ArgumentNullException.ThrowIfNull(decompressed);

        var graves = new List<ProspectGrave>();
        var tree = UePropertyReader.ReadStream(decompressed);
        var recorders = tree.FirstOrDefault(p =>
            string.Equals(p.Name, ProspectWorldReader.RecorderArray, StringComparison.Ordinal));
        if (recorders is null)
        {
            return graves;
        }

        for (var i = 0; i < recorders.Children.Count; i++)
        {
            var actor = recorders.Children[i];
            var row = ProspectCharacterReader.FindString(actor, decompressed, "StaticItemDataRowName")
                   ?? ProspectCharacterReader.FindString(actor, decompressed, "ItemStaticData")
                   ?? "";

            var kind = Classify(row);
            if (kind is null)
            {
                continue;
            }

            var slots = 0;
            ProspectCharacterReader.Walk(actor, n =>
            {
                if (string.Equals(n.Name, "Slots", StringComparison.Ordinal) &&
                    string.Equals(n.Type, "ArrayProperty", StringComparison.Ordinal))
                {
                    slots += n.Children.Count;
                }
            });

            var transform = ProspectCharacterReader.Find(actor, "ActorTransform");
            graves.Add(new ProspectGrave(
                kind.Value,
                row,
                ProspectCharacterReader.FindInt(actor, decompressed, "IcarusActorGUID") ?? -1,
                transform is null ? null : ProspectTransformReader.Read(decompressed, transform),
                slots,
                i));
        }

        return graves;
    }

    /// <summary>Maps an item row to a gravestone kind, or null when it is not a grave at all.</summary>
    public static GraveKind? Classify(string? rowName) => rowName switch
    {
        DownedRow => GraveKind.DownedBody,
        MissingRow => GraveKind.MissingInAction,
        _ => null,
    };
}
